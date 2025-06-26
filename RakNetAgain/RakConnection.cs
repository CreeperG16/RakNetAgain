using RakNetAgain.Packets;
using System.Net;

namespace RakNetAgain;

// An abstract class that represents a RakNet connection, to implement
// the same logic client and server side.
public abstract class RakConnection {
    public required IPEndPoint Endpoint { get; init; }
    public ushort MaxTransferUnit { get; protected set; } = 1492;
    public ConnectionStatus Status { get; protected set; } = ConnectionStatus.Connecting;

    public enum ConnectionStatus { Connecting, Connected, Disconnecting, Disconnected }

    private readonly List<uint> receivedFrameSequences = [];
    private readonly List<uint> lostFrameSequences = [];
    private readonly Dictionary<byte, uint> highestSequenceIndices = [];
    private readonly Dictionary<byte, uint> orderIndices = [];
    private readonly Dictionary<byte, uint> sequenceIndices = [];
    private uint reliableIndex = 0;
    private short fragmentIndex = 0;
    private uint outputSequence = 0;
    private int lastSequence = -1;

    private readonly Dictionary<byte, Dictionary<uint, Frame>> orderingQueue = [];
    private readonly Dictionary<short, Dictionary<int, Frame>> frameFragments = [];

    // To be able to retransmit them on Nack
    private readonly Dictionary<uint, Frame[]> outputBackup = [];

    // TODO: ConcurrentQueue? thread safety in general!
    private readonly Dictionary<Frame.FramePriority, Queue<Frame>> outgoingQueues = new() {
        [Frame.FramePriority.Immediate] = new(),
        [Frame.FramePriority.Normal] = new(),
    };

    public delegate void OnConnectListener();
    public event OnConnectListener OnConnect = delegate { };
    protected virtual void EmitOnConnect() => OnConnect?.Invoke();

    public delegate void OnDisconnectListener();
    public event OnDisconnectListener OnDisconnect = delegate { };
    protected virtual void EmitOnDisconnect() => OnDisconnect?.Invoke();

    public delegate void OnTickListener();
    public event OnTickListener OnTick = delegate { };
    protected virtual void EmitOnTick() => OnTick?.Invoke();

    public delegate void OnGamePacketListener(byte[] data);
    public event OnGamePacketListener OnGamePacket = delegate { };
    protected virtual void EmitOnGamePacket(byte[] data) => OnGamePacket?.Invoke(data);

    internal abstract Task Send(byte[] data);

    private async Task SendFrameSet(FrameSet frameSet) {
        outputBackup[frameSet.Sequence] = frameSet.Frames;
        await Send(frameSet.Write());
    }

    internal async Task Tick() {
        if (Status == ConnectionStatus.Disconnecting || Status == ConnectionStatus.Disconnected) return;

        if (receivedFrameSequences.Count > 0) {
            Ack ack = new() { Sequences = [.. receivedFrameSequences] };
            await Send(ack.Write());
            receivedFrameSequences.Clear();
        }

        if (lostFrameSequences.Count > 0) {
            Nack nack = new() { Sequences = [.. lostFrameSequences] };
            await Send(nack.Write());
            lostFrameSequences.Clear();
        }

        OnTick?.Invoke();

        // TODO: differentiate these somehow?
        // Not sure yet how to properly implement immediate
        await FlushFrameQueue(Frame.FramePriority.Immediate);
        await FlushFrameQueue(Frame.FramePriority.Normal);
    }

    // --- Receiving packets ---

    internal async Task HandleOnlinePacket(byte[] data) {
        var id = data[0] & 0xf0;
        // Console.WriteLine($"Received connected packet '0x{id:X2}' [{(PacketID)id}] ({data.Length}) from client.");

        switch ((PacketID)id) {
            case PacketID.Ack:
                HandleIncomingAck(data);
                break;
            case PacketID.Nack:
                HandleIncomingNack(data);
                break;
            case PacketID.FrameSet:
                await HandleFrameSet(data);
                break;
            default:
                break;
        }
    }

    private void HandleIncomingAck(byte[] data) {
        Ack ack = new(data);
        foreach (uint sequence in ack.Sequences) {
            outputBackup.Remove(sequence);
        }
    }

    private void HandleIncomingNack(byte[] data) {
        Nack nack = new(data);

        foreach (uint sequence in nack.Sequences) {
            // TODO: maybe log if a packet was lost (not found in the backup queue)?
            if (!outputBackup.TryGetValue(sequence, out var frames)) continue;
            foreach (Frame frame in frames) QueueFrame(frame, Frame.FramePriority.Immediate);
        }
    }

    private async Task HandleFrameSet(byte[] data) {
        FrameSet frameset = new(data);

        if (receivedFrameSequences.Contains(frameset.Sequence)) {
            Console.WriteLine($"Received duplicate frameset '{frameset.Sequence}'");
            return;
        }

        // Remove the sequence from the lost sequences
        lostFrameSequences.Remove(frameset.Sequence);

        // Check if we're out of order
        if ((int)frameset.Sequence <= lastSequence) {
            Console.WriteLine($"Received out of order frameset '{frameset.Sequence}' (last was {lastSequence})");
            // discard the frameset (todo?)
            return;
        }

        receivedFrameSequences.Add(frameset.Sequence);

        int difference = (int)frameset.Sequence - lastSequence;
        if (difference > 1) {
            for (int index = lastSequence + 1; index < (int)frameset.Sequence; index++) {
                // Add to queue (send nack on next tick)
                lostFrameSequences.Add((uint)index);
            }
        }

        lastSequence = (int)frameset.Sequence;

        foreach (Frame frame in frameset.Frames) {
            await HandleFrame(frame);
        }
    }

    private async Task HandleFrame(Frame frame) {
        if (frame.IsFragmented) {
            await HandleFrameFragment(frame);
            return;
        }

        if (frame.IsSequenced) {
            if (!highestSequenceIndices.TryGetValue(frame.OrderChannel, out var currentSequence)) {
                currentSequence = 0;
            }

            if (frame.SequenceIndex < currentSequence) {
                // Out of order
                Console.WriteLine($"Frame out of order, discarding (at IsSequenced)");
                return;
            }

            highestSequenceIndices[frame.OrderChannel] = frame.SequenceIndex;
            await HandleFramedPacket(frame.Payload);
            return;
        }

        if (frame.IsOrderedExclusive) {
            if (!orderIndices.TryGetValue(frame.OrderChannel, out var expectedIndex)) {
                expectedIndex = 0;
            }

            if (frame.OrderIndex > expectedIndex) {
                // Out of order
                if (!orderingQueue.TryGetValue(frame.OrderChannel, out var queue)) {
                    orderingQueue[frame.OrderChannel] = queue = [];
                }
                queue[frame.OrderIndex] = frame;
                return;
            }

            if (frame.OrderIndex == expectedIndex) {
                orderIndices[frame.OrderChannel] = frame.OrderIndex + 1;

                await HandleFramedPacket(frame.Payload);

                orderingQueue.TryGetValue(frame.OrderChannel, out var outOfOrderQueue);
                outOfOrderQueue ??= [];

                var index = orderIndices[frame.OrderChannel];
                while (outOfOrderQueue.ContainsKey(index)) {
                    await HandleFramedPacket(outOfOrderQueue[index].Payload);
                    outOfOrderQueue.Remove(index++);
                }

                orderingQueue[frame.OrderChannel] = outOfOrderQueue;
                orderIndices[frame.OrderChannel] = index;

                return;
            }

            return;
        }

        await HandleFramedPacket(frame.Payload);
    }

    private async Task HandleFrameFragment(Frame frame) {
        if (!frameFragments.TryGetValue(frame.FragmentId, out var fragments)) {
            // Add the ID to the dict
            frameFragments[frame.FragmentId] = new() { { frame.FragmentIndex, frame } };
            return; // Return since if a packet fits in one fragment, it wouldn't have been fragmented (...hopefully)
        }

        fragments[frame.FragmentIndex] = frame;

        // Check if we have all of the fragments to reconstruct the packet
        if (fragments.Count != frame.FragmentCount) return;

        List<byte> fullPayload = [];
        for (int index = 0; index < fragments.Count; index++) {
            fullPayload.AddRange(fragments[index].Payload);
        }

        // Construct a new (not fragmented) frame to be handled
        Frame fullFrame = new() {
            Reliability = frame.Reliability,
            ReliableIndex = frame.ReliableIndex,
            SequenceIndex = frame.SequenceIndex,
            OrderIndex = frame.OrderIndex,
            OrderChannel = frame.OrderChannel,
            Payload = [.. fullPayload],
        };

        // Handle the new frame
        frameFragments.Remove(frame.FragmentId);
        await HandleFrame(fullFrame);
    }

    private async Task HandleFramedPacket(byte[] data) {
        // Console.WriteLine($"Received framed packet '0x{data[0]:X2}' [{(PacketID)data[0]}] ({data.Length}) from client [{Status}].");
        // Console.WriteLine($"{BitConverter.ToString(data).Replace("-", " ")}");

        if (Status == ConnectionStatus.Connecting) {
            HandleUnconnectedPacket(data);
        } else {
            HandleConnectedPacket(data);
        }

        // Flush immediate packets (?)
        await FlushFrameQueue(Frame.FramePriority.Immediate);
    }

    // Implement these differently Client / Server side

    // Packets received before NewIncomingConnection
    protected abstract void HandleUnconnectedPacket(byte[] data);
    // Packets received after NewIncomingConnection
    protected abstract void HandleConnectedPacket(byte[] data);

    // --- Sending packets ---

    protected void QueueFrame(Frame frame, Frame.FramePriority priority) {
        if (!orderIndices.TryGetValue(frame.OrderChannel, out var currentOrder)) currentOrder = 0;
        if (!sequenceIndices.TryGetValue(frame.OrderChannel, out var currentSequence)) currentSequence = 0;

        if (frame.IsSequenced) {
            frame.OrderIndex = currentOrder;
            frame.SequenceIndex = currentSequence;
            sequenceIndices[frame.OrderChannel] = currentSequence + 1;
        } else if (frame.IsOrderedExclusive) {
            frame.OrderIndex = currentOrder;
            orderIndices[frame.OrderChannel] = currentOrder + 1;
            sequenceIndices[frame.OrderChannel] = 0;
        }

        // Split the frame payload if it's too big
        var maxSize = MaxTransferUnit - 29;
        if (frame.Payload.Length > maxSize) {
            QueueFrameFragments(frame, priority, maxSize);
        } else {
            frame.ReliableIndex = reliableIndex++;
            outgoingQueues[priority].Enqueue(frame);
        }

        // TODO: ?
        if (priority != Frame.FramePriority.Immediate) return;
        _ = FlushFrameQueue(Frame.FramePriority.Immediate);
    }

    private void QueueFrameFragments(Frame original, Frame.FramePriority priority, int maxSize) {
        short fragmentId = fragmentIndex++;
        int fragmentCount = (original.Payload.Length + maxSize - 1) / maxSize;

        for (int index = 0; index < fragmentCount; index++) {
            var slice = original.Payload[(index * maxSize)..Math.Min((index + 1) * maxSize, original.Payload.Length)];
            Frame fragment = new() {
                Reliability = Frame.FrameReliability.ReliableOrdered, // Set this to ordered / sequenced regardless of original frame's reliability to ensure the packet is reassembled
                ReliableIndex = reliableIndex++,
                SequenceIndex = original.SequenceIndex,
                OrderIndex = original.OrderIndex,
                OrderChannel = original.OrderChannel,
                FragmentCount = fragmentCount,
                FragmentId = fragmentId,
                FragmentIndex = index,
                Payload = slice,
            };

            outgoingQueues[priority].Enqueue(fragment);
        }
    }

    // Creates a FrameSet from a queue, sized <= MTU
    // An edge case might be if the queue contains a frame bigger than MTU but this should not happen
    // as adding to the queue happens only after fragmentation (in QueueFrame, QueueFrameFragment)
    private FrameSet FrameSetFromQueue(Frame.FramePriority priority) {
        var queue = outgoingQueues[priority];
        int sumLength = 0;
        List<Frame> frames = [];
        while (queue.Count > 0) {
            sumLength += queue.Peek().ByteSize;
            if (sumLength + 4 > MaxTransferUnit) break; // +4 for the packet ID (1) and the sequence (3)
            frames.Add(queue.Dequeue());
        }
        return new FrameSet {
            Sequence = outputSequence++,
            Frames = [.. frames],
        };
    }

    private async Task FlushFrameQueue(Frame.FramePriority priority) {
        var queue = outgoingQueues[priority];
        // lock (queue) { } // TODO: thread safety?
        if (queue.Count == 0) return;
        while (queue.Count > 0) {
            FrameSet frameSet = FrameSetFromQueue(priority);
            await SendFrameSet(frameSet);
        }
    }

    public virtual void Disconnect() {
        Disconnect packet = new();
        Frame frame = new() {
            Reliability = Frame.FrameReliability.Unreliable,
            OrderChannel = 0,
            Payload = packet.Write(),
        };

        QueueFrame(frame, Frame.FramePriority.Immediate);
    }
}
