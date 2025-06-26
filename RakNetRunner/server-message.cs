namespace RakNetRunner;

public class ServerMessage {
    public enum GameEdition {
        Minecraft,
        MinecraftEducation,
    };

    public enum GameMode {
        Survival,
        Creative,
        Adventure,
        Spectator,
    }

    private static readonly Dictionary<string, GameMode> modes = new() {
        { "Survival", GameMode.Survival },
        { "Creative", GameMode.Creative },
        { "Adventure", GameMode.Adventure },
        { "Spectator", GameMode.Spectator },
    };

    public GameEdition Edition = GameEdition.Minecraft;
    public string Motd = "-"; // Must be at least one character for Minecraft to recognise it
    public required int ProtocolVersion;
    public required string GameVersion;
    public int PlayerCount = 0;
    public int MaxPlayers = 0;
    public required ulong ServerGuid;
    public string Levelname = "";
    public GameMode Mode = GameMode.Survival;
    public required ushort Port;
    public required ushort PortV6;

    public override string ToString() {
        string value = "";

        value += Edition == GameEdition.Minecraft ? "MCPE;" : "MCEE;";
        value += Motd + ";";
        value += ProtocolVersion.ToString() + ";";
        value += GameVersion.ToString() + ";";
        value += PlayerCount.ToString() + ";";
        value += MaxPlayers.ToString() + ";";
        value += ServerGuid.ToString() + ";";
        value += Levelname + ";";
        value += Mode.ToString() + ";";
        value += "1;"; // Nintendo limited
        value += Port.ToString() + ";";
        value += PortV6.ToString() + ";";
        value += "0;"; // Unsure what this is, but BDS sets it

        return value;
    }

    public static ServerMessage FromString(string message) {
        var parts = message.Split(";");
        return new ServerMessage {
            Edition = parts[0] == "MCPE" ? GameEdition.Minecraft : GameEdition.MinecraftEducation,
            Motd = parts[1],
            ProtocolVersion = int.Parse(parts[2]),
            GameVersion = parts[3],
            PlayerCount = int.Parse(parts[4]),
            MaxPlayers = int.Parse(parts[5]),
            ServerGuid = ulong.Parse(parts[6]),
            Levelname = parts[7],
            Mode = modes[parts[8]],
            Port = ushort.Parse(parts[10]),
            PortV6 = ushort.Parse(parts[11]),
        };
    }
}
