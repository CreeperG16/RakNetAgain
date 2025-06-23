namespace RakNetAgain.Packets;

public class UnconnectedPing() {
    public static readonly PacketID Id = PacketID.UnconnectedPing;

    public readonly long Time;
    public readonly long ClientGuid;

    public UnconnectedPing(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        Time = reader.ReadInt64BE();
        reader.ReadMagic();
        ClientGuid = reader.ReadInt64BE();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.WriteBE(Time);
        writer.WriteMagic();
        writer.WriteBE(ClientGuid);

        return stream.ToArray();
    }
}
