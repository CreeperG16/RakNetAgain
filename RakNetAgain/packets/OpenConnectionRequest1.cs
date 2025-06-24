namespace RakNetAgain.Packets;

public class OpenConnectionRequest1() {
    public static readonly PacketID Id = PacketID.OpenConnectionRequest1;

    public byte ProtocolVersion { get; init; }
    public ushort MaxTransferUnit { get; init; }

    public OpenConnectionRequest1(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        reader.ReadMagic();
        ProtocolVersion = reader.ReadByte();
        MaxTransferUnit = (ushort)(stream.Length - stream.Position);
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteMagic();
        writer.Write(ProtocolVersion);
        writer.Write(new byte[MaxTransferUnit - 60]);

        return stream.ToArray();
    }
}
