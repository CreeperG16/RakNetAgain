using System.Text;

namespace RakNetAgain;

public class UnconnectedPong() {
    public static readonly PacketID Id = PacketID.UnconnectedPong;

    public long Time { get; init; }
    public ulong ServerGuid { get; init; }
    public string Message { get; init; } = "";

    public UnconnectedPong(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        Time = reader.ReadInt64();
        // ServerGuid = reader.ReadUInt64();
        ServerGuid = BitConverter.ToUInt64([.. reader.ReadBytes(8).Reverse()]);
        reader.ReadMagic();
        var len = reader.ReadUInt16();
        Message = Encoding.UTF8.GetString(reader.ReadBytes(len));
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id);

        writer.Write(Time);

        writer.Write(BitConverter.IsLittleEndian
            ? [.. BitConverter.GetBytes(ServerGuid).Reverse()]
            : BitConverter.GetBytes(ServerGuid));

        writer.WriteMagic();
        // writer.Write((ushort)Encoding.UTF8.GetByteCount(Message));
        var len = (ushort)Encoding.UTF8.GetByteCount(Message);
        writer.Write((byte)(len >> 8));
        writer.Write((byte)(len & 0xff));

        writer.Write(Encoding.UTF8.GetBytes(Message));

        return stream.ToArray();
    }
}
