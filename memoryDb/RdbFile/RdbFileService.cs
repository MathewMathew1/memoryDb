using System.Text;
using Microsoft.Extensions.Logging;
using RedisServer.Database.Model;
using RedisServer.Database.Service;
using RedisServer.ServerInfo.Service;
using RedisServer.Util.Serialization;

namespace RedisServer.RdbFile.Service
{
    public class RdbFileService : IRdbFileService
    {

        private ILogger<RdbFileService> _logger;
        private IServerInfoService _serverInfoService;
        private IStringService _stringService;
        private IStreamService _streamService;
        private IListDatabase _listService;

        public RdbFileService(ILogger<RdbFileService> logger, IServerInfoService serverInfoService, IStringService stringService,
        IStreamService streamService, IListDatabase listDatabase)
        {
            _logger = logger;
            _serverInfoService = serverInfoService;
            _stringService = stringService;
            _streamService = streamService;
            _listService = listDatabase;

            LoadRdbIntoMemory();
        }

        public List<string> GetKeys(string searchKey)
        {
            List<string> keys = new List<string>();

            var data = _serverInfoService.GetServerDataInfo();
            var fullPath = Path.Combine(data.dir, data.dbFileName);
          
            byte[] allBytes = File.ReadAllBytes(fullPath);
            _logger.LogInformation(BitConverter.ToString(allBytes).Replace("-", " "));

            if (!File.Exists(fullPath))
                return new List<string>();

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            var magic = Encoding.ASCII.GetString(reader.ReadBytes(5));
            var version = Encoding.ASCII.GetString(reader.ReadBytes(4));


            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int opCode = reader.ReadByte();

                switch (opCode)
                {
                    case 0xFF:
                        return keys;

                    case 0xFE:
                        _ = ByteRdbParser.ReadLengthEncodedInt(reader); // DB selector
                        continue;

                    case 0xFA:
                        ByteRdbParser.SkipLengthPrefixedString(reader); // AUX key
                        ByteRdbParser.SkipLengthPrefixedString(reader); // AUX value
                        continue;

                    case 0xFB: // RESIZEDB - skip two length-encoded integers
                        _ = ByteRdbParser.ReadLengthEncodedInt(reader);
                        _ = ByteRdbParser.ReadLengthEncodedInt(reader);
                        continue;

                    case 0xFC: // EXPIRETIME_MS - skip 8 bytes
                        _ = reader.ReadInt64();
                        continue;

                    case 0xFD: // EXPIRETIME - skip 4 bytes
                        _ = reader.ReadInt32();
                        continue;

                    case >= 0x00 and <= 0xFD:

                        string key = ByteRdbParser.ReadLengthPrefixedString(reader);

                        if (searchKey == "*" || key == searchKey)
                        {
                            keys.Add(key);
                        }
                        ByteRdbParser.ReadLengthPrefixedString(reader);

                        break;

                    default:
                        return keys;
                }
            }

            return keys;
        }


        public void LoadRdbIntoMemory()
        {
            var data = _serverInfoService.GetServerDataInfo();
            var fullPath = Path.Combine(data.dir, data.dbFileName);
              _logger.LogInformation("RDB path: " + Path.GetFullPath(Path.Combine(data.dir, data.dbFileName)));
            if (!File.Exists(fullPath)) return;
             _logger.LogInformation("is here");
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            _ = reader.ReadBytes(5); // "REDIS"
            _ = reader.ReadBytes(4); // version

            long? expireAtMs = null;

            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    long pos = reader.BaseStream.Position;
                    int opCode = reader.ReadByte();
                    _logger.LogDebug($"@{pos}: opcode {opCode:X2}");

                    switch (opCode)
                    {
                        case 0xFF:
                            return;

                        case 0xFE:
                            _ = ByteRdbParser.ReadLengthEncodedInt(reader);
                            break;

                        case 0xFA:
                            ByteRdbParser.SkipLengthPrefixedString(reader);
                            ByteRdbParser.SkipLengthPrefixedString(reader);
                            break;

                        case 0xFB:
                            _ = ByteRdbParser.ReadLengthEncodedInt(reader);
                            _ = ByteRdbParser.ReadLengthEncodedInt(reader);
                            break;

                        case 0xFD: // EXPIRETIME (seconds)
                            int seconds = reader.ReadInt32();
                            expireAtMs = DateTimeOffset.FromUnixTimeSeconds(seconds).ToUnixTimeMilliseconds();
                            break;

                        case 0xFC: // EXPIRETIME_MS (milliseconds)
                            long ms = reader.ReadInt64();
                            expireAtMs = ms;
                            break;

                        case 0x02: // list
                            GetLists(reader);
                            break;

                        case 0x15: // Stream
                            GetSteam(reader);
                            break;

                        default:
                            if (opCode >= 0x00 && opCode <= 0xFD)
                            {
                                string key = ByteRdbParser.ReadLengthPrefixedString(reader);
                                string value = ByteRdbParser.ReadLengthPrefixedString(reader);

                                double? ttl = null;
                                if (expireAtMs.HasValue)
                                {
                                    long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                    long delta = expireAtMs.Value - nowMs;

                                    if (delta < 0)
                                    {
                                        _logger.LogInformation($"Key '{key}' expired during load. Skipping.");
                                        expireAtMs = null;
                                        continue;
                                    }

                                    ttl = delta > 0 ? (double?)delta : 1;
                                }

                                _stringService.Set(key, value, new SetKeyParameters { expirationTime = ttl });

                                expireAtMs = null;
                            }
                            else
                            {
                                throw new InvalidDataException($"Unknown opcode: 0x{opCode:X2} at position {pos}");
                            }

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"RDB Load Error @ {reader.BaseStream.Position}: {e}");
            }
        }

        private void GetLists(BinaryReader reader)
        {
            string key = ByteRdbParser.ReadLengthPrefixedString(reader);
            //int listLen = reader.ReadInt32();
            int listLen = ByteRdbParser.ReadLengthEncodedInt(reader);

            if (listLen < 0)
            {
                _logger.LogWarning($"Invalid list length {listLen} for key '{key}'");
                return;
            }
            _logger.LogDebug($"@{key}: key");
            for (int i = 0; i < listLen; i++)
            {
                string value = ByteRdbParser.ReadLengthPrefixedString(reader);
                _listService.AddRight(key, value);
            }

            _logger.LogInformation($"Loaded list '{key}' with {listLen} elements.");
        }

        private void GetSteam(BinaryReader reader)
        {

            string key = ByteRdbParser.ReadLengthPrefixedString(reader);

            int groupCount = ByteRdbParser.ReadLengthEncodedInt(reader);
            if (groupCount == 0) return;

            for (int i = 0; i < groupCount; i++)
            {
                string baseId = ByteRdbParser.ReadStreamId(reader);
                int entryCount = ByteRdbParser.ReadLengthEncodedInt(reader);
                int fieldCount = ByteRdbParser.ReadLengthEncodedInt(reader);

                var fieldNames = new List<string>();
                for (int j = 0; j < fieldCount; j++)
                {
                    fieldNames.Add(ByteRdbParser.ReadLengthPrefixedString(reader));
                }

                var baseParts = baseId.Split('-');
                long baseMs = long.Parse(baseParts[0]);
                int baseSeq = int.Parse(baseParts[1]);

                for (int k = 0; k < entryCount; k++)
                {
                    int seqDelta = ByteRdbParser.ReadLengthEncodedInt(reader);
                    int actualSeq = baseSeq + seqDelta;

                    string fullId = $"{baseMs}-{actualSeq}";

                    var fields = new Dictionary<string, string>();
                    foreach (var field in fieldNames)
                    {
                        string value = ByteRdbParser.ReadLengthPrefixedString(reader);
                        fields[field] = value;
                    }

                    _streamService.AddEntry(key, new StreamEntry
                    {
                        Id = fullId,
                        Fields = fields
                    });


                }
            }

        }




    }
}