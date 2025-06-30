using System.Text;
using RedisServer.Database.Model;

namespace RedisServer.Util.Serialization
{
    public static class RespBuilder
    {
        public static void AppendStreamEntries(StringBuilder builder, List<StreamEntry> entries)
        {
            builder.Append($"*{entries.Count}\r\n");

            foreach (var entry in entries)
            {
                builder.Append($"*2\r\n");
                builder.Append($"${entry.Id.Length}\r\n{entry.Id}\r\n");

                builder.Append($"*{entry.Fields.Count * 2}\r\n");

                foreach (var field in entry.Fields)
                {
                    builder.Append($"${field.Key.Length}\r\n{field.Key}\r\n");
                    builder.Append($"${field.Value.Length}\r\n{field.Value}\r\n");
                }
            }
        }

        public static void StreamEntriesRead(StringBuilder builder, List<Task<List<StreamEntry>?>> readTasks, List<string> idKeys)
        {
            var nonEmptyStreams = new List<(string key, List<StreamEntry> entries)>();

            for (int i = 0; i < idKeys.Count; i++)
            {
                var entries = readTasks[i].Result;
                if (entries != null && entries.Count > 0)
                    nonEmptyStreams.Add((idKeys[i], entries));
            }

            if (nonEmptyStreams.Count == 0)
            {
                builder.Append("$-1\r\n"); 
                return;
            }

            builder.Append($"*{nonEmptyStreams.Count}\r\n");

            foreach (var (key, entries) in nonEmptyStreams)
            {
                builder.Append("*2\r\n");
                builder.Append($"${key.Length}\r\n{key}\r\n");

                builder.Append($"*{entries.Count}\r\n");

                foreach (var entry in entries)
                {
                    builder.Append("*2\r\n");
                    builder.Append($"${entry.Id.Length}\r\n{entry.Id}\r\n");
                    builder.Append($"*{entry.Fields.Count * 2}\r\n");

                    foreach (var field in entry.Fields)
                    {
                        builder.Append($"${field.Key.Length}\r\n{field.Key}\r\n");
                        builder.Append($"${field.Value.Length}\r\n{field.Value}\r\n");
                    }
                }
            }
        }

    }
}
