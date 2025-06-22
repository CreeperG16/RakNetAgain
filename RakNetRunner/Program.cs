using RakNetAgain;

namespace RakNetRunner;

public class Program {
    public static void Main() {
        RakServer server = new(25565);

        Console.WriteLine($"Starting server...");
        server.Start();
    }
}
