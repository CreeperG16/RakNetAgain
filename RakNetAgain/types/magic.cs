namespace RakNetAgain.Types;

public static class Magic {
    public static readonly int Size = 16;
    public static readonly byte[] Value = [
        0x00, 0xff, 0xff, 0x00,
        0xfe, 0xfe, 0xfe, 0xfe,
        0xfd, 0xfd, 0xfd, 0xfd,
        0x12, 0x34, 0x56, 0x78
    ];

    public static void WriteMagic(this BinaryWriter writer) => writer.Write(Value);

    public static void ReadMagic(this BinaryReader reader) {
        byte[] actual = reader.ReadBytes(Size);

        if (actual.Length != Size) throw new InvalidDataException("Magic: Unexpected end of stream");
        for (int i = 0; i < Size; i++) {
            if (actual[i] != Value[i]) throw new InvalidDataException("Magic: Invalid byte in magic value");
        }
    }
}
