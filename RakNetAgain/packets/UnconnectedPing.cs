using RakNetAgain.Types;
using System.Diagnostics.CodeAnalysis;

namespace RakNetAgain.Packets;

public class UnconnectedPing() {
    public static readonly PacketID Id = PacketID.UnconnectedPing;

    public required long Time { get; init; }
    public required ulong ClientGuid { get; init; }

    [SetsRequiredMembers]
    public UnconnectedPing(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        Time = reader.ReadInt64BE();
        reader.ReadMagic();
        ClientGuid = reader.ReadUInt64BE();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteBE(Time);
        writer.WriteMagic();
        writer.WriteBE(ClientGuid);

        return stream.ToArray();
    }
}
