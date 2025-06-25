namespace RakNetAgain.Packets;

public class Ack() {
    public static readonly PacketID Id = PacketID.Ack;

    public uint[] Sequences { get; init; } = [];

    public Ack(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        Sequences = reader.ReadSequenceRecords();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteSequenceRecords(Sequences);

        return stream.ToArray();
    }
}
