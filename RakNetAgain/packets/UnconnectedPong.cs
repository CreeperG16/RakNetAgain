using System.Text;

namespace RakNetAgain;

public class UnconnectedPong() {
    public static readonly PacketID Id = PacketID.UnconnectedPing;

    public long Time { get; init; }
    public long ServerGuid { get; init; }
    public string Message { get; init; } = "";

    public UnconnectedPong(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        Time = reader.ReadInt64();
        ServerGuid = reader.ReadInt64();
        reader.ReadMagic();
        var len = reader.ReadUInt16();
        Message = Encoding.UTF8.GetString(reader.ReadBytes(len));
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(Time);
        writer.Write(ServerGuid);
        writer.WriteMagic();
        writer.Write((ushort)Encoding.UTF8.GetByteCount(Message));
        writer.Write(Encoding.UTF8.GetBytes(Message));

        return stream.ToArray();
    }
}
