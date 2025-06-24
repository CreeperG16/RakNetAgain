using System.Net;

namespace RakNetAgain.Packets;

public class OpenConnectionRequest2() {
    public static readonly PacketID Id = PacketID.OpenConnectionRequest2;

    public int Cookie { get; init; }
    public bool HasSecurityChallenge { get; init; }
    public IPEndPoint? ServerAddress { get; init; }
    public short MaxTransferUnit { get; init; }
    public ulong ClientGuid { get; init; }

    public OpenConnectionRequest2(byte[] data, bool hasCookie) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        reader.ReadMagic();
        if (hasCookie) {
            Cookie = reader.ReadInt32();
            HasSecurityChallenge = reader.ReadBoolean();
        }
        ServerAddress = reader.ReadAddress();
        MaxTransferUnit = reader.ReadInt16BE();
        ClientGuid = reader.ReadUInt64BE();
    }

    public byte[] Write(bool hasCookie) {
        if (ServerAddress == null) throw new MissingFieldException("OpenConnectionRequest2: ServerAddress field not provided!");

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteMagic();
        if (hasCookie) {
            writer.Write(Cookie);
            writer.Write(HasSecurityChallenge);
        }
        writer.WriteAddress(ServerAddress);
        writer.WriteBE(MaxTransferUnit);
        writer.WriteBE(ClientGuid);

        return stream.ToArray();
    }
}
