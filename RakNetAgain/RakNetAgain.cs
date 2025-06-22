using System.Net;
using System.Net.Sockets;

namespace RakNetAgain;

public class RakServer(short port) {
    private readonly short SERVER_PORT = port;
    private readonly UdpClient socket = new(port);

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

                var msg = $"MCPE;Very server;818;1.21.92;23;99;3253860892328930865;Ledrock bevel;Creative;1;25565;25566;0;"; // TODO

                var pong = new UnconnectedPong {
                    Time = packet.Time,
                    ServerGuid = 3253860892328930865,
                    Message = msg,
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
