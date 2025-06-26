using RakNetAgain.Types;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace RakNetAgain.Packets;

// The fields in this packet are pretty unknown, however it isn't really needed for just a server implementation
public class NewIncomingConnection() {
    public static readonly PacketID Id = PacketID.NewIncomingConnection;

    public required IPEndPoint ServerAddress { get; init; }
    public required IPEndPoint[] InternalAddresses { get; init; } = new IPEndPoint[20]; // ???
    public required long IncomingTime { get; init; }
    public required long ServerTime { get; init; }

    [SetsRequiredMembers]
    public NewIncomingConnection(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        ServerAddress = reader.ReadAddress();
        InternalAddresses = [.. Enumerable.Range(0, 20).Select(_ => reader.ReadAddress())];
        IncomingTime = reader.ReadInt64BE();
        ServerTime = reader.ReadInt64BE();
    }

    public byte[] Write() {
        if (InternalAddresses.Length != 20)
            throw new MissingFieldException("NewIncomingConnection: InternalAddresses field incorrect! (must be 20 internal addresses)");

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteAddress(ServerAddress);
        foreach (var addr in InternalAddresses) writer.WriteAddress(addr);
        writer.WriteBE(IncomingTime);
        writer.WriteBE(ServerTime);

        return stream.ToArray();
    }
}
