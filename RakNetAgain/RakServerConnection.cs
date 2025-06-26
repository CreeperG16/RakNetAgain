using RakNetAgain.Packets;

namespace RakNetAgain;

public class RakServerConnection : RakConnection {
    private readonly RakServer _server;
    internal RakServerConnection(RakServer server, ushort? mtu = null) {
        _server = server;
        MaxTransferUnit = mtu ?? MaxTransferUnit;
    }

    internal override Task Send(byte[] data) => _server.SendPacket(data, Endpoint);

    protected override void HandleUnconnectedPacket(byte[] data) {
        switch ((PacketID)data[0]) {
            case PacketID.Disconnect:
                Status = ConnectionStatus.Disconnecting;
                EmitOnDisconnect();
                Status = ConnectionStatus.Disconnected;
                break;
            case PacketID.ConnectionRequest:
                HandleConnectionRequest(data);
                break;
            case PacketID.NewIncomingConnection:
                Status = ConnectionStatus.Connected;
                // Console.WriteLine($"Client connected: {Endpoint}");
                EmitOnConnect();
                break;
        }
    }

    protected override void HandleConnectedPacket(byte[] data) {
        switch ((PacketID)data[0]) {
            case PacketID.Disconnect:
                Status = ConnectionStatus.Disconnecting;
                EmitOnDisconnect();
                Status = ConnectionStatus.Disconnected;
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

    private void HandleConnectionRequest(byte[] data) {
        ConnectionRequest packet = new(data);

        ConnectionRequestAccepted reply = new() {
            ClientAddress = Endpoint,
            SystemIndex = (short)_server.Connections.Count, // TODO: Properly?
            RequestTime = packet.Time,
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        Frame frame = new() {
            Reliability = Frame.FrameReliability.Unreliable,
            Payload = reply.Write(),
        };

        QueueFrame(frame, Frame.FramePriority.Normal);
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

        QueueFrame(frame, Frame.FramePriority.Normal);
    }

    public void SendGamePacket(byte[] data, Frame.FramePriority priority) {
        Frame frame = new() {
            Reliability = Frame.FrameReliability.Reliable,
            Payload = [0xfe, .. data],
        };

        QueueFrame(frame, priority);
    }
}
