using System.Text;
using Microsoft.Extensions.Logging;
using RedisServer.Database.Model;
using RedisServer.Database.Service;
using RedisServer.ServerInfo.Service;
using RedisServer.Util.Serialization;

namespace RedisServer.RdbFile.Service
{
    public class RdbFileBuilderService : IRdbFileBuilderService
    {
        private readonly IServerInfoService _serverInfoService;
        private readonly IStringService _stringService;
        private readonly IStreamService _steamService;
        private readonly IListDatabase _listDatabase;
        private readonly ILogger<RdbFileBuilderService> _logger;
        private readonly ISetService _setService;

        private const string RDB_MAGIC = "REDIS";
        private const string RDB_VERSION = "0009";

        public RdbFileBuilderService(IServerInfoService serverInfoService, IStringService stringService,
        ILogger<RdbFileBuilderService> logger, IStreamService streamService, IListDatabase listDatabase,
        ISetService setService)
        {
            _serverInfoService = serverInfoService;
            _stringService = stringService;
            _logger = logger;
            _steamService = streamService;
            _listDatabase = listDatabase;
            _setService = setService;
        }

        public void WriteRdbFromMemory()
        {
            var data = _serverInfoService.GetServerDataInfo();

            var fullPath = Path.Combine(data.dir, data.dbFileName);

            using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);

            writer.Write(Encoding.ASCII.GetBytes(RDB_MAGIC));
            writer.Write(Encoding.ASCII.GetBytes(RDB_VERSION));

            HandleStrings(writer);
            HandleSteams(writer);
            HandleList(writer);
            HandleSets(writer);

            writer.Write((byte)0xFF); // EOF
            writer.Flush();
        }

        private void HandleList(BinaryWriter writer)
        {
            var allLists = _listDatabase.GetSnapshot();

            foreach (var kvp in allLists)
            {
                var key = kvp.Key;
                var list = kvp.Value.GetSnapshot();

                if (list.Count == 0)
                    continue;
                writer.Write((byte)0x02);
                RdbCoder.WriteLengthPrefixedString(writer, key);
                RdbCoder.WriteLength(writer, list.Count);

                foreach (var item in list)
                {
                    RdbCoder.WriteLengthPrefixedString(writer, item);
                }
            }

        }

        private void HandleSteams(BinaryWriter writer)
        {
            var streams = _steamService.GetAllSnapshot();

            foreach (var pair in streams)
            {

                writer.Write((byte)0x15);

                RdbCoder.WriteLengthPrefixedString(writer, pair.Key);

                var entries = pair.Value.Range("", null).ToList();

                RdbCoder.WriteLength(writer, entries.Count);
                if (entries.Count < 1) continue;


                var lastId = entries[entries.Count - 1].Key;

                Dictionary<string, List<KeyValuePair<string, StreamEntry>>> timeGroups = new();

                foreach (var entry in entries)
                {
                    var parts = entry.Key.Split('-');
                    var milliseconds = parts[0];

                    if (!timeGroups.TryGetValue(milliseconds, out var list))
                    {
                        list = new List<KeyValuePair<string, StreamEntry>>();
                        timeGroups[milliseconds] = list;
                    }

                    list.Add(entry);
                }

                foreach (var item in timeGroups)
                {
                    var group = item.Value;

                    RdbCoder.WriteStreamListpack(writer, group[0].Key, group);
                }


            }
        }

        private void HandleSets(BinaryWriter writer)
        {
            var sets = _setService.GetAllSnapshot();

            foreach (var set in sets)
            {

                writer.Write((byte)0x03); 

                RdbCoder.WriteLengthPrefixedString(writer, set.Key);

                var entries = set.Value.GetAll().ToList();

                RdbCoder.WriteLength(writer, entries.Count);

                foreach (var entry in entries)
                {

                    RdbCoder.WriteLengthPrefixedString(writer, entry.Key);
                    RdbCoder.WriteDouble(writer, entry.Value); 
                }



            }
        }

        private void HandleStrings(BinaryWriter writer)
        {
            var strings = GetSnapshot();

            foreach (var pair in strings)
            {
                if (pair.Value.expirationDate is DateTime exp)
                {
                    long expireMs = new DateTimeOffset(exp).ToUnixTimeMilliseconds();
                    writer.Write((byte)0xFC);
                    writer.Write(expireMs);
                }

                writer.Write((byte)0x00);

                RdbCoder.WriteLengthPrefixedString(writer, pair.Key);
                RdbCoder.WriteLengthPrefixedString(writer, pair.Value.value);
            }
        }

        private Dictionary<string, ValueInMemory> GetSnapshot()
        {
            if (_stringService is StringService concrete)
            {
                lock (concrete.SyncRoot)
                {
                    return concrete.GetAllSnapshot();
                }
            }
            throw new InvalidOperationException("Underlying service does not support snapshot.");
        }
    }
}

