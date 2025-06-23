// #error version

using RakNetAgain.Packets;
using System.Net;
using System.Net.Sockets;

namespace RakNetAgain;

public class RakServer(ushort port) {
    private readonly ushort SERVER_PORT = port;
    private readonly UdpClient socket = new(port);

    public readonly ulong Guid = (ulong)new Random().NextInt64();

    public ushort MaxTransferUnit { get; init; } = 1492;

    public int MaxPlayers { get; set; } = 10;
    public int ProtocolVersion { get; set; } = 818;
    public string GameVersion { get; set; } = "1.21.90";

    public static readonly byte RAKNET_VERSION = 11;

    // TODO: proper start func, threads?
    public void Start() {
        StartListener().Wait();
    }

    private CancellationTokenSource? _cts;
    public async Task StartListener(CancellationToken? cancellationToken = null) {
        _cts = new CancellationTokenSource();
        var token = cancellationToken ?? _cts.Token;

        while (!token.IsCancellationRequested) {
            await GetPackets();
            Thread.Sleep(30);
        }
    }

    public void StopListener() {
        _cts?.Cancel();
    }

    public async Task GetPackets() {
        try {
            UdpReceiveResult result = await socket.ReceiveAsync();
            await HandlePacket((PacketID)result.Buffer[0], result.Buffer[1..], result.RemoteEndPoint);
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task HandlePacket(PacketID id, byte[] data, IPEndPoint client) {
        Console.WriteLine($"Got RakNet packet: {id}");
        // Console.WriteLine(BitConverter.ToString(data).Replace("-", " "));

        switch (id) {
            case PacketID.UnconnectedPing:
                await HandleUnconnectedPing(data, client);
                break;
            case PacketID.OpenConnectionRequest1:
                await HandleOpenConnReq1(data, client);
                break;
            case PacketID.OpenConnectionRequest2:
                await HandleOpenConnReq2(data, client);
                break;
            default:
                break;
        }
    }

    private async Task HandleUnconnectedPing(byte[] data, IPEndPoint client) {
        UnconnectedPing packet = new(data);

        var pong = new UnconnectedPong {
            Time = packet.Time,
            ServerGuid = Guid,
            Message = new ServerMessage {
                GameVersion = GameVersion,
                ProtocolVersion = ProtocolVersion,
                Port = SERVER_PORT,
                PortV6 = (ushort)(SERVER_PORT + 1), // TODO: ?
                ServerGuid = Guid,

                PlayerCount = 0, // TODO: current connections
                MaxPlayers = MaxPlayers,
            },
        };

        await SendPacket(pong.Write(), client);
    }

    private async Task HandleOpenConnReq1(byte[] data, IPEndPoint client) {
        OpenConnectionRequest1 packet = new(data);
        if (packet.ProtocolVersion != RAKNET_VERSION) {
            IncompatibleProtocol incompatible = new() {
                ProtocolVersion = RAKNET_VERSION,
                ServerGuid = Guid,
            };

            await SendPacket(incompatible.Write(), client);
            return;
        }

        ushort mtu = Math.Min(MaxTransferUnit, packet.MaxTransferUnit);

        OpenConnectionReply1 reply = new() {
            ServerGuid = Guid,
            UseSecurity = false,
            MaxTransferUnit = mtu,
        };

        await SendPacket(reply.Write(), client);
    }

    private async Task HandleOpenConnReq2(byte[] data, IPEndPoint client) {
        OpenConnectionRequest2 packet = new(data, false);

        OpenConnectionReply2 reply = new() {
            ServerGuid = Guid,
            ClientAddress = client,
            MaxTransferUnit = packet.MaxTransferUnit,
            EncryptionEnabled = false,
        };

        // TODO: make connection class here

        await SendPacket(reply.Write(), client);
    }

    private async Task SendPacket(byte[] data, IPEndPoint client) {
        await socket.SendAsync(data, data.Length, client);
    }

    public void Close() {
        socket.Close();
    }
}
