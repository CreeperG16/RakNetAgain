namespace RakNetAgain.Packets;

public class UnconnectedPong() {
    public static readonly PacketID Id = PacketID.UnconnectedPong;

    public long Time { get; init; }
    public ulong ServerGuid { get; init; }
    public ServerMessage? Message { get; init; }

    public UnconnectedPong(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        Time = reader.ReadInt64BE();
        ServerGuid = reader.ReadUInt64BE();
        reader.ReadMagic();
        Message = ServerMessage.FromString(reader.ReadString16());
    }

    public byte[] Write() {
        if (Message == null) throw new MissingFieldException("UnconnectedPong: Message field not provided!");

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);
        writer.WriteBE(Time);
        writer.WriteBE(ServerGuid);
        writer.WriteMagic();
        writer.WriteString16(Message.ToString());

        return stream.ToArray();
    }
}
