using RakNetAgain.Types;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace RakNetAgain.Packets;

public class OpenConnectionReply2() {
    public static readonly PacketID Id = PacketID.OpenConnectionReply2;

    public required ulong ServerGuid { get; init; }
    public required IPEndPoint ClientAddress { get; init; }
    public required short MaxTransferUnit { get; init; }
    public required bool EncryptionEnabled { get; init; }

    [SetsRequiredMembers]
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
