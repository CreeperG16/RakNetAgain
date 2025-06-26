using RakNetAgain;
using System.Net;

namespace RakNetRunner;

public class Program {
    public static void Main() {
        // StartServer();
        StartClient();
        // StartProxy();
    }

    public static void StartServer() {
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

    public static void StartClient() {
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

    public static void StartProxy() {
        RakServer server = new(port: 8080) { MaxConnections = 1 };

        Console.WriteLine($"server starting on 8080");

        server.OnDiscovery += (endpoint) => {
            Console.WriteLine($"SERVER: Ping from {endpoint} (Currently {server.Connections.Count} connections)");

            ServerMessage discoveryMessage = new() {
                GameVersion = "1.21.92",
                ProtocolVersion = 818,
                Port = 8080,
                PortV6 = 8081,
                ServerGuid = server.Guid,
                PlayerCount = server.Connections.Count,
                MaxPlayers = server.MaxConnections,
            };

            return discoveryMessage.ToString();
        };

        RakClient client = new(host: "127.0.0.1", port: 25565);

        client.OnConnect += () => {
            Console.WriteLine($"Finished connecting to upstream [S].");
        };

        server.OnConnect += (connection) => {
            Console.WriteLine($"{connection.Endpoint} New connection...");

            // ---

            connection.OnConnect += () => {
                Console.WriteLine($"Finished connecting to downstream [C].");
            };

            // ---
            client.OnGamePacket += (packet) => {
                Console.WriteLine($"Got packet from upstream server");
                Console.WriteLine($"[{BitConverter.ToString(packet).Replace("-", " ")}]");
                connection.SendGamePacket(packet, Frame.FramePriority.Immediate);
            };

            connection.OnGamePacket += (packet) => {
                Console.WriteLine($"Got packet from downstream client");
                Console.WriteLine($"[{BitConverter.ToString(packet).Replace("-", " ")}]");
                client.SendGamePacket(packet, Frame.FramePriority.Immediate);
            };

            // ---
            client.OnDisconnect += () => {
                Console.WriteLine($"Upstream server requested disconnect.");
                connection.Disconnect();
            };

            connection.OnDisconnect += () => {
                Console.WriteLine($"Downstream client requested disconnect.");
                client.Disconnect();
            };
        };

        Task.WaitAll(
            server.Start(),
            client.Connect()
        );
    }
}
