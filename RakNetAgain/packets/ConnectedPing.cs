namespace RakNetAgain.Packets;

public class ConnectedPing() {
    public static readonly PacketID Id = PacketID.ConnectedPing;

    public long Time { get; init; }

    public ConnectedPing(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        Time = reader.ReadInt64BE();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteBE(Time);

        return stream.ToArray();
    }
}
