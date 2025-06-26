using RakNetAgain.Packets;
using RakNetAgain.Types;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace RakNetAgain;

public class RakClient : RakConnection {
    private static readonly byte RAKNET_VERSION = 11;

    private readonly UdpClient socket = new();
    private readonly Dictionary<long, Action<UnconnectedPong>> unconnectedPings = [];

    public readonly ulong Guid = (ulong)new Random().NextInt64();

    [SetsRequiredMembers]
    public RakClient(string host, ushort port, ushort? mtu = null) {
        Endpoint = new(IPAddress.Parse(host), port);
        MaxTransferUnit = mtu ?? MaxTransferUnit;
        socket.Connect(Endpoint);
    }

    internal override Task Send(byte[] data) => socket.SendAsync(data, data.Length);

    // TODO: timeout? + don't assume the very next packet received is going to be the pong!
    private async Task<UnconnectedPong> PingServer() {
        TaskCompletionSource<UnconnectedPong> tcs = new();
        UnconnectedPing ping = new() {
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ClientGuid = Guid,
        };
        await Send(ping.Write());
        unconnectedPings[ping.Time] = (pong) => {
            tcs.SetResult(pong);
            unconnectedPings.Remove(ping.Time);
        };
        return await tcs.Task;
        // return new UnconnectedPong(reply.Buffer);
    }

    // TODO: proper connect func, threads?
    private CancellationTokenSource? _cts;
    public async Task Connect(CancellationToken? cancellationToken = null) {
        _cts = new CancellationTokenSource();
        var token = cancellationToken ?? _cts.Token;

        await Task.WhenAll(
            StartListener(token),
            StartConnectionTicking(token),
            Connect()
        );
    }

    public override void Disconnect() {
        base.Disconnect();
        _cts?.Cancel();
        socket.Close();
    }

    private async Task Connect() {
        // TODO: fail on ping timeout
        var pong = await PingServer();
        // ServerGuid = pong.ServerGuid; // TODO: save the server's GUID somewhere?

        Console.WriteLine($"Pong message: '{pong.Message}'");

        OpenConnectionRequest1 request = new() {
            ProtocolVersion = RAKNET_VERSION,
            MaxTransferUnit = MaxTransferUnit,
        };

        await Send(request.Write());
    }

    private CancellationTokenSource? _listenerCts;
    public async Task StartListener(CancellationToken? cancellationToken = null) {
        _listenerCts = new CancellationTokenSource();
        var token = cancellationToken ?? _listenerCts.Token;

        while (!token.IsCancellationRequested) {
            await GetPackets();
            await Task.Delay(30, token);
        }
    }

    private CancellationTokenSource? _connectionTickerCts;
    public async Task StartConnectionTicking(CancellationToken? cancellationToken = null) {
        _connectionTickerCts = new CancellationTokenSource();
        var token = cancellationToken ?? _connectionTickerCts.Token;

        while (!token.IsCancellationRequested) {
            await Tick();
            await Task.Delay(100, token);
        }
    }

    public void StopListener() => _listenerCts?.Cancel();
    public void StopConnectionTicking() => _connectionTickerCts?.Cancel();

    public async Task GetPackets() {
        try {
            UdpReceiveResult result = await socket.ReceiveAsync();
            await HandleOfflinePacket(result.Buffer);
        } catch (Exception ex) {
            Console.WriteLine($"Error receiving packets: {ex.Message}");
        }
    }

    private async Task HandleOfflinePacket(byte[] data) {
        byte id = data[0];

        if ((id & 128) != 0) {
            await HandleOnlinePacket(data);
            return;
        }

        switch ((PacketID)id) {
            case PacketID.UnconnectedPong:
                HandleUnconnectedPong(data);
                break;
            case PacketID.IncompatibleProtocolVersion:
                IncompatibleProtocol packet = new(data);
                throw new Exception($"Client's protocol version is incompatible with server (Server expects version {packet.ProtocolVersion}, Client version is {RAKNET_VERSION})");
            case PacketID.OpenConnectionReply1:
                await HandleOpenConnReply1(data);
                break;
            case PacketID.OpenConnectionReply2:
                HandleOpenConnReply2(data);
                break;
            default:
                break;
        }
    }

    private void HandleUnconnectedPong(byte[] data) {
        UnconnectedPong pong = new(data);
        if (unconnectedPings.TryGetValue(pong.Time, out var pingCallback)) pingCallback(pong);
    }

    private async Task HandleOpenConnReply1(byte[] data) {
        OpenConnectionReply1 packet = new(data);
        MaxTransferUnit = packet.MaxTransferUnit;

        OpenConnectionRequest2 request = new() {
            ServerAddress = Endpoint,
            MaxTransferUnit = (short)MaxTransferUnit,
            ClientGuid = Guid,
        };
        await Send(request.Write(hasCookie: false));
    }

    private void HandleOpenConnReply2(byte[] data) {
        OpenConnectionReply2 packet = new(data);
        MaxTransferUnit = (ushort)packet.MaxTransferUnit;

        ConnectionRequest request = new() {
            ClientGuid = Guid,
            Time = DateTimeOffset.UnixEpoch.ToUnixTimeMilliseconds(),
        };

        Frame frame = new() {
            Reliability = Frame.FrameReliability.Unreliable,
            Payload = request.Write(),
        };

        QueueFrame(frame, Frame.FramePriority.Normal);
    }

    protected override void HandleUnconnectedPacket(byte[] data) {
        switch ((PacketID)data[0]) {
            case PacketID.Disconnect:
                Status = ConnectionStatus.Disconnecting;
                EmitOnDisconnect();
                Status = ConnectionStatus.Disconnected;
                break;
            case PacketID.ConnectionRequestAccepted:
                HandleConnectionRequestAccepted(data);
                break;
        }
    }

    protected override void HandleConnectedPacket(byte[] data) {
        // Console.WriteLine($"got packet '{data[0]:X2}' [{(PacketID)data[0]}]");

        switch ((PacketID)data[0]) {
            case PacketID.Disconnect:
                Status = ConnectionStatus.Disconnecting;
                EmitOnDisconnect();
                Status = ConnectionStatus.Disconnected;
                break;
            case PacketID.ConnectedPong:
                // TODO: ?
                break;
            case PacketID.ConnectedPing:
                HandleConnectedPing(data);
                break;
            case PacketID.GamePacket:
                // Console.WriteLine($"GOT GAME PACKET WOOOO");
                EmitOnGamePacket(data[1..]); // Trim off the 0xfe game packet id
                break;
        }
    }

    private void HandleConnectionRequestAccepted(byte[] data) {
        ConnectionRequestAccepted packet = new(data);

        NewIncomingConnection reply = new() {
            IncomingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ServerAddress = Endpoint,
            InternalAddresses = [.. Enumerable.Range(0, 20).Select(_ => new IPEndPoint(0, 19132))],
            ServerTime = packet.Time,
        };

        Frame frame = new() {
            Reliability = Frame.FrameReliability.Reliable,
            Payload = reply.Write(),
        };

        ConnectedPing ping = new() {
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        Frame pingFrame = new() {
            Reliability = Frame.FrameReliability.Unreliable,
            Payload = ping.Write(),
        };

        QueueFrame(frame, Frame.FramePriority.Normal);
        QueueFrame(pingFrame, Frame.FramePriority.Normal);

        Status = ConnectionStatus.Connected;
        EmitOnConnect();
    }

    public void SendGamePacket(byte[] data, Frame.FramePriority priority) {
        Frame frame = new() {
            Reliability = Frame.FrameReliability.Reliable,
            Payload = [0xfe, .. data],
        };

        QueueFrame(frame, priority);
    }

    private void HandleConnectedPing(byte[] data) {
        ConnectedPing ping = new(data);
        ConnectedPong pong = new() {
            PingTime = ping.Time,
            PongTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        Frame frame = new() {
            Reliability = Frame.FrameReliability.Unreliable,
            Payload = pong.Write(),
        };

        QueueFrame(frame, Frame.FramePriority.Immediate);
    }

    // Static method for pinging servers without creating a new Client
    // TODO: timeout here too? also the receiveasync stuff feels jank
    public static async Task<UnconnectedPong> PingServer(string host, ushort port) {
        UdpClient sock = new();
        sock.Connect(host, port);

        UnconnectedPing packet = new() {
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ClientGuid = (ulong)new Random().NextInt64(),
        };

        byte[] data = packet.Write();

        var replyTask = sock.ReceiveAsync();
        await sock.SendAsync(data, data.Length);

        UnconnectedPong pong = new((await replyTask).Buffer);

        return pong;
    }
}
