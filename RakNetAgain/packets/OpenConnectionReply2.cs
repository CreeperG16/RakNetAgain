using System.Net;

namespace RakNetAgain.Packets;

public class OpenConnectionReply2() {
    public static readonly PacketID Id = PacketID.OpenConnectionReply2;

    public ulong ServerGuid { get; init; }
    public IPEndPoint? ClientAddress { get; init; }
    public short MaxTransferUnit { get; init; }
    public bool EncryptionEnabled { get; init; }

    public OpenConnectionReply2(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if (id != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        reader.ReadMagic();
        ServerGuid = reader.ReadUInt64BE();
        ClientAddress = reader.ReadAddress();
        MaxTransferUnit = reader.ReadInt16BE();
        EncryptionEnabled = reader.ReadBoolean();
    }

    public byte[] Write() {
        if (ClientAddress == null) throw new MissingFieldException("OpenConnectionReply2: ClientAddress field not provided!");

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteMagic();
        writer.WriteBE(ServerGuid);
        writer.WriteAddress(ClientAddress);
        writer.Write(MaxTransferUnit);
        writer.Write(EncryptionEnabled);

        return stream.ToArray();
    }
}
