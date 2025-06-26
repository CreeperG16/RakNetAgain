using RakNetAgain;

namespace RakNetRunner;

public class Program {
    public static void Main() {
        RakServer server = new(25565) {
            MaxConnections = 10,
            ProtocolVersion = 818,
            GameVersion = "1.21.92",
        };

        server.OnConnect += (connection) => {
            Console.WriteLine($"New connection wooo");
            connection.OnConnect += () => {
                Console.WriteLine($"{connection.Endpoint} Connected yay");
            };
            connection.OnGamePacket += (packet) => {
                Console.WriteLine($"Got a game packet wooo");
                Console.WriteLine($"{BitConverter.ToString(packet).Replace("-", " ")}");
            };
        };

        Console.WriteLine($"Starting server (guid: {server.Guid})...");
        server.Start();
    }
}
