using System.Diagnostics.CodeAnalysis;

namespace RakNetAgain.Packets;

public class OpenConnectionReply1() {
    public static readonly PacketID Id = PacketID.OpenConnectionReply1;

    public required ulong ServerGuid { get; init; }
    public required bool UseSecurity { get; init; }
    public int Cookie { get; init; }
    public required ushort MaxTransferUnit { get; init; }

    [SetsRequiredMembers]
    public OpenConnectionReply1(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        reader.ReadMagic();
        ServerGuid = reader.ReadUInt64BE();
        UseSecurity = reader.ReadBoolean();
        if (UseSecurity) Cookie = reader.ReadInt32();
        MaxTransferUnit = reader.ReadUInt16BE();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteMagic();
        writer.WriteBE(ServerGuid);
        writer.Write(UseSecurity);
        if (UseSecurity) writer.Write(Cookie);
        writer.WriteBE(MaxTransferUnit);

        return stream.ToArray();
    }
}
