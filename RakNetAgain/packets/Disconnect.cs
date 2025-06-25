namespace RakNetAgain.Packets;

public class Disconnect() {
    public static readonly PacketID Id = PacketID.Disconnect;

    public Disconnect(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);

        return stream.ToArray();
    }
}
