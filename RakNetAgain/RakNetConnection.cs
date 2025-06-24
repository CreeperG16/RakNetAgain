using RakNetAgain.Packets;
using System.Net;

namespace RakNetAgain;

public class RakConnection {
    public required IPEndPoint Endpoint { get; init; }
    public required ushort MaxTransferUnit { get; init; }
    public ConnectionStatus Status { get; private set; }

    public enum ConnectionStatus { Connecting, Connected, Disconnecting, Disconnected }

    private readonly List<uint> receivedFrameSequences = [];
    private readonly List<uint> lostFrameSequences = [];
    private readonly Dictionary<byte, uint> highestSequenceIndices = [];
    private readonly Dictionary<byte, uint> orderIndices = [];
    private readonly Dictionary<byte, uint> sequenceIndices = [];
    private uint reliableIndex = 0;
    private short fragmentIndex = 0;
    private uint outputSequence = 0;

    private readonly Dictionary<byte, Dictionary<uint, Frame>> orderingQueue = [];
    private readonly Dictionary<short, Dictionary<int, Frame>> frameFragments = [];

    private readonly Dictionary<Frame.FramePriority, Queue<Frame>> outgoingQueues = new() {
        [Frame.FramePriority.Immediate] = new(),
        [Frame.FramePriority.Normal] = new(),
    };

    private int lastSequence = -1;

    internal RakConnection() { }

    internal async Task HandleIncomingFrameSet(byte[] data) {
        var id = data[0] & 0xf0;
        Console.WriteLine($"Received connected packet '0x{id:X2}' [{(PacketID)id}] ({data.Length}) from client.");

        switch ((PacketID)id) {
            case PacketID.Ack:
                break;
            case PacketID.Nack:
                break;
            case PacketID.FrameSet:
                await HandleFrameSet(data);
                break;
            default:
                break;
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

    // Handle packets that arrived in Frames
    private async Task HandlePacket(byte[] data) {
        byte header = data[0];

        Console.WriteLine($"Received framed packet '0x{header:X2}' ({data.Length}) from client.");

        // TODO: this if statement is probably redundant
        if (Status == ConnectionStatus.Connecting) {
            switch ((PacketID)header) {
                case PacketID.Disconnect:
                    Status = ConnectionStatus.Disconnecting;
                    // TODO: call callbacks / event
                    Status = ConnectionStatus.Disconnected;
                    break;
                case PacketID.ConnectionRequest:
                    HandleConnectionRequest(data);
                    break;
                case PacketID.NewIncomingConnection:
                    Status = ConnectionStatus.Connected;
                    Console.WriteLine($"Client connected: {Endpoint}");
                    break;
            }
        } else {
            switch ((PacketID)header) {
                case PacketID.Disconnect:
                    Status = ConnectionStatus.Disconnecting;
                    // TODO: callback / event
                    Status = ConnectionStatus.Disconnected;
                    break;
                case PacketID.ConnectedPing:
                    // TODO: send pong
                    break;
                case PacketID.GamePacket:
                    // TODO: callback / event (user-facing)
                    break;
            }
        }

        // Flush immediate packets (?)
        await FlushFrameQueue(Frame.FramePriority.Immediate);
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
            await HandlePacket(frame.Payload);
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

                await HandlePacket(frame.Payload);

                orderingQueue.TryGetValue(frame.OrderChannel, out var outOfOrderQueue);
                outOfOrderQueue ??= [];

                var index = orderIndices[frame.OrderChannel];
                while (outOfOrderQueue.ContainsKey(index)) {
                    await HandlePacket(outOfOrderQueue[index].Payload);
                    outOfOrderQueue.Remove(index++);
                }

                orderingQueue[frame.OrderChannel] = outOfOrderQueue;
                orderIndices[frame.OrderChannel] = index;

                return;
            }

            return;
        }

        await HandlePacket(frame.Payload);
    }

    private async Task HandleFrameFragment(Frame frame) {
        if (!frameFragments.ContainsKey(frame.FragmentId)) {
            // Add the ID to the dict
            frameFragments[frame.FragmentId] = new() { { frame.FragmentIndex, frame } };
            return;
        }

        var fragments = frameFragments[frame.FragmentId];
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

    private void QueueFrame(Frame frame, Frame.FramePriority priority) {
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
    }

    private void QueueFrameFragments(Frame original, Frame.FramePriority priority, int maxSize) {
        short fragmentId = fragmentIndex++;
        int fragmentCount = (original.Payload.Length + maxSize - 1) / maxSize;

        for (int index = 0; index < fragmentCount; index++) {
            var slice = original.Payload[(index * maxSize)..Math.Min((index + 1) * maxSize, original.Payload.Length)];
            Frame fragment = new() {
                Reliability = original.Reliability, // TODO: set this to ordered / sequenced regardless of original frame's reliability?
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
        while (true) {
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
        if (queue.Count == 0) return;
        while (queue.Count > 0) {
            FrameSet frameSet = FrameSetFromQueue(priority);
            await SendFrameSet(frameSet);
        }
    }

    private void HandleConnectionRequest(byte[] data) {
        ConnectionRequest packet = new(data);

        ConnectionRequestAccepted reply = new() {
            ClientAddress = Endpoint,
            SystemIndex = 0, // TODO: increment?
            RequestTime = packet.Time,
            Time = 0, // TODO: now.
        };

        Frame frame = new() {
            Reliability = Frame.FrameReliability.Unreliable,
            OrderChannel = 0,
            Payload = reply.Write(),
        };

        QueueFrame(frame, Frame.FramePriority.Normal);
    }

    private async Task SendFrameSet(FrameSet frameSet) {
        // TODO: get the socket into this class somehow?
        await Task.Delay(0);
    }
}
