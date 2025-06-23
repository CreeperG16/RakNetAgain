// #error version

using System.Net;
using System.Net.Sockets;

namespace RakNetAgain;

public class RakServer(ushort port) {
    private readonly ushort SERVER_PORT = port;
    private readonly UdpClient socket = new(port);
    
    public ulong Guid { get; } = (ulong)new Random().NextInt64();

    public int MaxPlayers { get; set; } = 10;
    public int ProtocolVersion { get; set; } = 818;
    public string GameVersion { get; set; } = "1.21.90";

    public static readonly int RakNetProtocol = 11;

    // TODO: proper start func, threads?
    public void Start() {
        StartListener().Wait();
    }

    private async Task StartListener() {
        while (true) {
            try {
                UdpReceiveResult result = await socket.ReceiveAsync();
                await HandlePacket((PacketID)result.Buffer[0], result.Buffer[1..], result.RemoteEndPoint);
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private async Task HandlePacket(PacketID id, byte[] data, IPEndPoint client) {
        Console.WriteLine($"Got RakNet packet: {id}");
        // Console.WriteLine(BitConverter.ToString(data).Replace("-", " "));

        switch (id) {
            case PacketID.UnconnectedPing:
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

                break;
            default:
                break;
        }
    }

    private async Task SendPacket(byte[] data, IPEndPoint client) {
        await socket.SendAsync(data, data.Length, client);
    }
}
