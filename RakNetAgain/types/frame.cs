namespace RakNetAgain;

public class Frame() {
    public enum FrameReliability {
        Unreliable,
        UnreliableSequenced,
        Reliable,
        ReliableOrdered,
        ReliableSequenced,
        UnreliableWithAckReceipt,
        ReliableWithAckReceipt,
        ReliableOrderedWithAckReceipt,
    }

    public enum FramePriority { Normal, Immediate }

    public FrameReliability Reliability;
    public uint /* lu24 */ ReliableIndex; // Only if reliable
    public uint /* lu24 */ SequenceIndex; // Only if sequenced
    public uint /* lu24 */ OrderIndex; // Only if ordered
    public byte OrderChannel; // Only if ordered

    // Only if fragmented
    public int /*  li32 */ FragmentCount = 0;
    public short /* i16 */ FragmentId;
    public int /*  li32 */ FragmentIndex;

    public byte[] Payload = [];

    public bool IsFragmented => FragmentCount > 0;
    public bool IsReliable =>
        Reliability != FrameReliability.Unreliable &&
        Reliability != FrameReliability.UnreliableSequenced &&
        Reliability != FrameReliability.UnreliableWithAckReceipt;
    public bool IsSequenced =>
        Reliability == FrameReliability.UnreliableSequenced ||
        Reliability == FrameReliability.ReliableSequenced;
    public bool IsOrdered =>
        IsSequenced ||
        Reliability == FrameReliability.ReliableOrdered ||
        Reliability == FrameReliability.ReliableOrderedWithAckReceipt;
    public bool IsOrderedExclusive => IsOrdered && !IsSequenced;

    public int ByteSize {
        get {
            int length = 3; // Header + reliability
            length += Payload.Length; // Body length

            if (IsReliable) length += 3; // Reliable index
            if (IsSequenced) length += 3; // Sequence index
            if (IsOrdered) length += 3 + 1; // Order index, order channel
            if (IsFragmented) length += 4 + 2 + 4; // fragment size, id, index
            return length;
        }
    }

    public Frame(BinaryReader reader) : this() {
        // "Top 3 bits are reliability type, fourth bit is 1 when the frame is fragmented and part of a compound"
        byte flags = reader.ReadByte();
        Reliability = (FrameReliability)((flags >> 5) & 7);

        ushort bitLength = reader.ReadUInt16BE();
        var length = (bitLength + 7) >> 3;

        if (IsReliable) ReliableIndex = reader.ReadUInt24();
        if (IsSequenced) SequenceIndex = reader.ReadUInt24();
        if (IsOrdered) {
            OrderIndex = reader.ReadUInt24();
            OrderChannel = reader.ReadByte();
        }

        // Fragmented
        if ((flags & 0b0010000) > 0) {
            FragmentCount = reader.ReadInt32BE();
            FragmentId = reader.ReadInt16BE();
            FragmentIndex = reader.ReadInt32BE();
        }

        Payload = reader.ReadBytes(length);
    }

    public byte[] Write() {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        var flags = ((byte)Reliability << 5) | (IsFragmented ? 16 : 0);
        writer.Write((byte)flags);

        writer.WriteBE((ushort)(Payload.Length << 3));

        if (IsReliable) writer.WriteUInt24(ReliableIndex);
        if (IsSequenced) writer.WriteUInt24(SequenceIndex);
        if (IsOrdered) {
            writer.WriteUInt24(OrderIndex);
            writer.Write(OrderChannel);
        }

        if (IsFragmented) {
            writer.WriteBE(FragmentCount);
            writer.WriteBE(FragmentId);
            writer.WriteBE(FragmentIndex);
        }

        writer.Write(Payload);

        return stream.ToArray();
    }
}
