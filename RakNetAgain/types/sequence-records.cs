namespace RakNetAgain.Types;

public static class SequenceRecords {
    private struct SequenceRecord(bool si, uint s, uint e) {
        public bool Single = si;
        public uint Start = s;
        public uint End = e;
    }

    private static SequenceRecord[] RecordsFromSequences(uint[] sequences) {
        List<SequenceRecord> records = [];
        SequenceRecord currentRecord = new(true, 0, 0);

        for (int i = 0; i < sequences.Length; i++) {
            uint sequence = sequences[i];

            if (i == 0) {
                // Initialise current record
                currentRecord = new(true, sequence, sequence);
                continue;
            }

            if (currentRecord.End == sequence) {
                // Skip duplicate sequence
                continue;
            }

            if (currentRecord.End == sequence - 1) {
                // Extend the current record
                currentRecord.Single = false;
                currentRecord.End = sequence;
                continue;
            }

            // Finalise the current record and start a new one
            records.Add(currentRecord);
            currentRecord = new(true, sequence, sequence);
        }

        records.Add(currentRecord);
        return [.. records];
    }

    private static uint[] SequencesFromRecords(SequenceRecord[] records) {
        List<uint> sequences = [];
        Queue<SequenceRecord> recordQueue = new(records);

        while (recordQueue.Count > 0) {
            var current = recordQueue.Dequeue();
            if (current.Single) {
                sequences.Add(current.Start);
            } else {
                for (uint seq = current.Start; seq < current.End; seq++) {
                    sequences.Add(seq);
                }
            }
        }

        return [.. sequences];
    }

    public static void WriteSequenceRecords(this BinaryWriter writer, uint[] sequences) {
        SequenceRecord[] records = RecordsFromSequences(sequences);
        writer.WriteBE((short)records.Length);
        foreach (var record in records) {
            writer.Write(record.Single);
            writer.WriteUInt24(record.Start);
            if (!record.Single) writer.WriteUInt24(record.End);
        }
    }

    public static uint[] ReadSequenceRecords(this BinaryReader reader) {
        List<SequenceRecord> records = [];
        short recordCount = reader.ReadInt16BE();
        for (var i = 0; i < recordCount; i++) {
            bool isSingle = reader.ReadBoolean();
            uint start = reader.ReadUInt24();
            SequenceRecord record = new() {
                Single = isSingle,
                Start = start,
                End = isSingle ? start : reader.ReadUInt24(),
            };
            records.Add(record);
        }

        return SequencesFromRecords([.. records]);
    }
}
