using System.Net;
using System.Net.Sockets;

namespace RakNetAgain;

public class RakServer(short port) {
    private readonly short SERVER_PORT = port;
    private readonly UdpClient socket = new(port);

    private async Task StartListener() {
        while (true) {
            try {
                UdpReceiveResult result = await socket.ReceiveAsync();
                await HandlePacket((PacketID)result.Buffer[0], result.Buffer, result.RemoteEndPoint);
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private async Task HandlePacket(PacketID id, byte[] data, IPEndPoint client) {
        switch (id) {
            case PacketID.UnconnectedPing:
                UnconnectedPing packet = new(data);

                var msg = $"MCPE;Very server;818;1.21.92;23;99;13253860892328930865;Ledrock Bevel;Creative;1;{SERVER_PORT};{SERVER_PORT + 1};"; // TODO

                var pong = new UnconnectedPong {
                    Time = packet.Time,
                    ServerGuid = 0,
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
