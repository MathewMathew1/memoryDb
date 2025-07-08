
using System.Text;
using RedisServer.Database.Model;

namespace RedisServer.Util.Serialization
{
    public static class RdbCoder
    {
        public static void WriteStreamListpack(BinaryWriter writer, string baseId, List<KeyValuePair<string, StreamEntry>> group)
        {
            var baseParts = baseId.Split('-');
            long baseMs = long.Parse(baseParts[0]);
            int baseSeq = int.Parse(baseParts[1]);

            WriteLengthPrefixedString(writer, baseId);
            WriteLength(writer, group.Count);

            var fieldNames = group[0].Value.Fields.Keys.ToList();
            WriteLength(writer, fieldNames.Count);

            foreach (var field in fieldNames)
                WriteLengthPrefixedString(writer, field);

            foreach (var entry in group)
            {
                var currentFields = entry.Value.Fields.Keys.ToList();
                if (!currentFields.SequenceEqual(fieldNames))
                    throw new InvalidDataException($"Inconsistent fields in group: expected [{string.Join(",", fieldNames)}], got [{string.Join(",", currentFields)}]");
            }

            foreach (var entry in group)
            {
                var parts = entry.Key.Split('-');
                long ms = long.Parse(parts[0]);
                int seq = int.Parse(parts[1]);

                if (ms != baseMs)
                    throw new InvalidOperationException("Timestamps in group must match");

                int delta = seq - baseSeq;
                WriteLength(writer, delta);

                foreach (var field in fieldNames)
                    WriteLengthPrefixedString(writer, entry.Value.Fields[field]);
            }
        }



        public static void StoreSteamId(string lastId, BinaryWriter writer)
        {
            var idParts = lastId.Split('-');
            ulong ms = ulong.Parse(idParts[0]);
            ulong seq = ulong.Parse(idParts[1]);

            WriteUInt64BigEndian(writer, ms);
            WriteUInt64BigEndian(writer, seq);
        }

        public static void WriteUInt64BigEndian(BinaryWriter writer, ulong value)
        {
            var buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            writer.Write(buffer);
        }





        public static void WriteLengthPrefixedString(BinaryWriter writer, string s)
        {
            byte[] data = Encoding.UTF8.GetBytes(s);
            WriteLength(writer, data.Length);
            writer.Write(data);
        }

        public static void WriteLength(BinaryWriter writer, int length)
        {
            if (length < 1 << 6)
            {
                writer.Write((byte)length);
            }
            else if (length < 1 << 14)
            {
                writer.Write((byte)((length >> 8) | 0x40));
                writer.Write((byte)(length & 0xFF));
            }
            else
            {
                writer.Write((byte)0x80);
                byte[] bytes = BitConverter.GetBytes(length);
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                writer.Write(bytes);
            }
        }

        public static void WriteDouble(BinaryWriter writer, double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes); 

            writer.Write(bytes);
        }

    }
}