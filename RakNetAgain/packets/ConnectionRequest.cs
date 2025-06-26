using System.Diagnostics.CodeAnalysis;

namespace RakNetAgain.Packets;

public class ConnectionRequest() {
    public static readonly PacketID Id = PacketID.ConnectionRequest;

    public required ulong ClientGuid { get; init; }
    public required long Time { get; init; }

    [SetsRequiredMembers]
    public ConnectionRequest(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        ClientGuid = reader.ReadUInt64BE();
        Time = reader.ReadInt64BE();
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteBE(ClientGuid);
        writer.WriteBE(Time);

        return stream.ToArray();
    }
}
