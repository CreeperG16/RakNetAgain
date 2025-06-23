namespace RakNetAgain.Packets;

public class IncompatibleProtocol() {
    public static readonly PacketID Id = PacketID.IncompatibleProtocolVersion;

    public byte ProtocolVersion { get; init; }
    public ulong ServerGuid { get; init; }

    public IncompatibleProtocol(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        ProtocolVersion = reader.ReadByte();
        reader.ReadMagic();
        ServerGuid = reader.ReadUInt64BE();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(ProtocolVersion);
        writer.WriteMagic();
        writer.WriteBE(ServerGuid);

        return stream.ToArray();
    }
}
