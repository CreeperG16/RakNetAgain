using System.Net;

namespace RakNetAgain.Packets;

public class ConnectionRequestAccepted() {
    public static readonly PacketID Id = PacketID.ConnectionRequestAccepted;

    public IPEndPoint? ClientAddress { get; init; }
    public short SystemIndex { get; init; }
    public long RequestTime { get; init; }
    public long Time { get; init; }

    public ConnectionRequestAccepted(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        ClientAddress = reader.ReadAddress();
        SystemIndex = reader.ReadInt16BE();

        // TODO: ?
        for (int i = 0; i < 10; i++) {
            reader.ReadAddress();
        }

        RequestTime = reader.ReadInt64BE();
        Time = reader.ReadInt64BE();
    }

    public byte[] Write() {
        if (ClientAddress == null) throw new MissingFieldException("ConnectionRequestAccepted: ClientAddress field not provided!");

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteAddress(ClientAddress);
        writer.WriteBE(SystemIndex);

        // TODO: ?
        for (int i = 0; i < 10; i++) {
            writer.WriteAddress(new IPEndPoint(0x00000000, 19132));
        }

        writer.WriteBE(RequestTime);
        writer.WriteBE(Time);

        return stream.ToArray();
    }
}
