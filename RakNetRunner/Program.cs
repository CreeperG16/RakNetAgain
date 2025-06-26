using RakNetAgain;

namespace RakNetRunner;

public class Program {
    public static void Main() {
        // MainServer();
        MainClient();
    }

    public static void MainServer() {
        RakServer server = new(25565) { MaxConnections = 10 };

        server.OnDiscovery += (endpoint) => {
            Console.WriteLine($"Ping from {endpoint} (Currently {server.Connections.Count} connections)");

            ServerMessage discoveryMessage = new() {
                GameVersion = "1.21.92",
                ProtocolVersion = 818,
                Port = 25565,
                PortV6 = 25566,
                ServerGuid = server.Guid,
                PlayerCount = server.Connections.Count,
                MaxPlayers = server.MaxConnections,
            };

            return discoveryMessage.ToString();
        };

        server.OnConnect += (connection) => {
            Console.WriteLine($"{connection.Endpoint} New connection...");
            connection.OnConnect += () => {
                Console.WriteLine($"{connection.Endpoint} Finished connecting.");
            };
            connection.OnGamePacket += (packet) => {
                Console.WriteLine($"Received a game packet from {connection.Endpoint}");
                Console.WriteLine($"{BitConverter.ToString(packet).Replace("-", " ")}");
            };
            connection.OnDisconnect += () => {
                Console.WriteLine($"{connection.Endpoint} disconnected.");
            };
        };

        Console.WriteLine($"Starting server (guid: {server.Guid})...");
        server.Start().Wait();
    }

    public static void MainClient() {
        Console.WriteLine($"Attempting to connect client...");
        RakClient client = new(host: "127.0.0.1", port: 19132);

        byte[] REQNETSET = [0x06, 0xC1, 0x01, 0x00, 0x00, 0x03, 0x32];

        client.OnConnect += () => {
            Console.WriteLine($"Client connected!");
            client.SendGamePacket(REQNETSET, Frame.FramePriority.Normal);
        };

        client.OnDisconnect += () => {
            Console.WriteLine($"Client disconnected.");
        };

        client.OnGamePacket += (data) => {
            Console.WriteLine($"Got game packet! [{BitConverter.ToString(data).Replace("-", " ")}]");
        };

        client.Connect().Wait();
    }
}
