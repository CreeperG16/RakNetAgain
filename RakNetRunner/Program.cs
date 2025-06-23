using RakNetAgain;

namespace RakNetRunner;

public class Program {
    public static void Main() {
        RakServer server = new(25565) {
            MaxPlayers = 10,
            ProtocolVersion = 818,
            GameVersion = "1.21.92",
        };

        Console.WriteLine($"Starting server (guid: {server.Guid})...");
        server.Start();
    }
}
