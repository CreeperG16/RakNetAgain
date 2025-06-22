namespace RakNetAgain;

public class UnconnectedPing() {
    public static readonly PacketID Id = PacketID.UnconnectedPing;

    public readonly long Time;
    public readonly long ClientGuid;

    public UnconnectedPing(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        Time = reader.ReadInt64();
        reader.ReadMagic();
        ClientGuid = reader.ReadInt64();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(Time);
        writer.WriteMagic();
        writer.Write(ClientGuid);

        return stream.ToArray();
    }
}
