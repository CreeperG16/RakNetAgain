using RakNetAgain.Types;
using System.Diagnostics.CodeAnalysis;

namespace RakNetAgain.Packets;

public class IncompatibleProtocol() {
    public static readonly PacketID Id = PacketID.IncompatibleProtocolVersion;

    public required byte ProtocolVersion { get; init; }
    public required ulong ServerGuid { get; init; }

    [SetsRequiredMembers]
    public IncompatibleProtocol(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

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
