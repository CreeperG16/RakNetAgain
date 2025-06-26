using RakNetAgain;

namespace RakNetRunner;

public class Program {
    public static void Main() {
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
        server.Start();
    }
}
