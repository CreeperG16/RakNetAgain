using RakNetAgain.Types;
using System.Diagnostics.CodeAnalysis;

namespace RakNetAgain.Packets;

public class UnconnectedPong() {
    public static readonly PacketID Id = PacketID.UnconnectedPong;

    public required long Time { get; init; }
    public required ulong ServerGuid { get; init; }
    public required string Message { get; init; }

    [SetsRequiredMembers]
    public UnconnectedPong(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        Time = reader.ReadInt64BE();
        ServerGuid = reader.ReadUInt64BE();
        reader.ReadMagic();
        Message = reader.ReadString16();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteBE(Time);
        writer.WriteBE(ServerGuid);
        writer.WriteMagic();
        writer.WriteString16(Message);

        return stream.ToArray();
    }
}
