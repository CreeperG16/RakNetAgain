using System.Diagnostics.CodeAnalysis;

namespace RakNetAgain.Packets;

public class ConnectedPong() {
    public static readonly PacketID Id = PacketID.ConnectedPong;

    public required long PingTime { get; init; }
    public required long PongTime { get; init; }

    [SetsRequiredMembers]
    public ConnectedPong(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        PingTime = reader.ReadInt64BE();
        PongTime = reader.ReadInt64BE();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteBE(PingTime);
        writer.WriteBE(PongTime);

        return stream.ToArray();
    }
}
