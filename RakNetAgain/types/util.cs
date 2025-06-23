using System.Text;

namespace RakNetAgain;

public static class BigEndianExtensions {
    public static void WriteBE(this BinaryWriter writer, ushort value) {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value & 0xff));

        // writer.Write([.. BitConverter.GetBytes(value).Reverse()]);
    }

    public static void WriteBE(this BinaryWriter writer, short value) => writer.WriteBE((ushort)value);

    public static void WriteBE(this BinaryWriter writer, uint value) {
        for (int i = 3; i >= 0; i--) {
            var val = value >> (8 * i);
            writer.Write((byte)(val & 0xff));
        }
    }

    public static void WriteBE(this BinaryWriter writer, int value) => writer.WriteBE((uint)value);

    public static void WriteBE(this BinaryWriter writer, ulong value) {
        for (int i = 7; i >= 0; i--) {
            var val = value >> (8 * i);
            writer.Write((byte)(val & 0xff));
        }
    }

    public static void WriteBE(this BinaryWriter writer, long value) => writer.WriteBE((ulong)value);

    // reader
    public static ushort ReadUInt16BE(this BinaryReader reader) {
        byte high = reader.ReadByte();
        byte low = reader.ReadByte();
        return (ushort)((high << 8) | low);
    }

    public static short ReadInt16BE(this BinaryReader reader) => (short)reader.ReadUInt16BE();

    public static uint ReadUInt32BE(this BinaryReader reader) {
        uint value = 0;
        for (int i = 3; i >= 0; i--) {
            value |= (uint)reader.ReadByte() << (8 * i);
        }
        return value;
    }

    public static int ReadInt32BE(this BinaryReader reader) => (int)reader.ReadUInt32BE();

    public static ulong ReadUInt64BE(this BinaryReader reader) {
        ulong value = 0;
        for (int i = 7; i >= 0; i--) {
            value |= (ulong)reader.ReadByte() << (8 * i);
        }
        return value;
    }

    public static long ReadInt64BE(this BinaryReader reader) => (long)reader.ReadUInt64BE();

    // UInt24LE
    public static void WriteUInt24(this BinaryWriter writer, uint value) {
        if (value < 0 || value > 0xFFFFFF)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 0xFFFFFF.");

        writer.Write((byte)(value & 0xff));
        writer.Write((byte)((value >> 8) & 0xff));
        writer.Write((byte)((value >> 16) & 0xff));
    }

    public static uint ReadUInt24(this BinaryReader reader) {
        uint value = 0;
        value |= reader.ReadByte();
        value |= (uint)reader.ReadByte() << 8;
        value |= (uint)reader.ReadByte() << 16;
        return value;
    }

    // Strings
    public static void WriteString16(this BinaryWriter writer, string value) {
        ushort len = (ushort)Encoding.UTF8.GetByteCount(value);
        writer.WriteBE(len);
        writer.Write(Encoding.UTF8.GetBytes(value));
    }

    public static string ReadString16(this BinaryReader reader) {
        ushort len = reader.ReadUInt16BE();
        byte[] bytes = reader.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }
}
