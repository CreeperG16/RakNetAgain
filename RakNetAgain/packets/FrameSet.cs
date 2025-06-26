using System.Diagnostics.CodeAnalysis;

namespace RakNetAgain.Packets;

public class FrameSet() {
    public static readonly PacketID Id = PacketID.FrameSet;

    public required uint /* lu24 */ Sequence { get; init; }
    public required Frame[] Frames { get; init; } = [];

    [SetsRequiredMembers]
    public FrameSet(byte[] data) : this() {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        var id = reader.ReadByte();
        if ((id & 0xf0) != (byte)Id) throw new InvalidDataException($"Unexpected packet ID '0x{id:X2}' when attempting to read {ToString()}.");

        Sequence = reader.ReadUInt24();

        List<Frame> frames = [];
        while (stream.Position < stream.Length) {
            frames.Add(new Frame(reader));
        }

        Frames = [.. frames];
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)Id); // Packet ID, TODO (Can be 0x80..0x8d?)
        writer.WriteUInt24(Sequence);
        foreach (Frame frame in Frames) writer.Write(frame.Write());

        return stream.ToArray();
    }
}
