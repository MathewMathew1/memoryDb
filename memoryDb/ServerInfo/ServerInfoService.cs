using Microsoft.Extensions.Logging;
using RedisServer.ServerInfo.Model;

namespace RedisServer.ServerInfo.Service
{
    public class ServerInfoService : IServerInfoService
    {
        private Role _role = Role.MASTER;
        public int port = 6379;
        private string _replicationId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
        private string? _masterHost;
        private int? _masterPort;
        private string _dir = "/tmp";
        private string _dbFileName = "rdbfile";
        private string? _authPassword = null;

        public ServerInfoService(string[] args)
        {

            var flagHandlers = new Dictionary<string, Action<List<string>>>
            {
                ["--port"] = value =>
                {
                    if (int.TryParse(value[0], out var ms))
                        port = ms;
                },
                ["--replicaof"] = value =>
                {

                    var splitString = value[0].Split(' ');
                    if (splitString.Length > 1)
                    {
                        _role = Role.SLAVE;
                        _masterHost = splitString[0];
                        _masterPort = int.Parse(splitString[1]);
                    }

                },
                ["--dir"] = value =>
                {
                    _dir = value[0];

                },
                ["--dbfilename"] = value =>
                {
                    _dbFileName = value[0];

                },
                ["--authpass"] = value =>
                {
                    _authPassword = value[0];
                }
            };

            for (int i = 0; i + 1 < args.Length; i += 2)
            {

                var flag = args[i].ToLowerInvariant();

                var values = new List<string> { };
                for (var a = i + 1; a < args.Length; a++)
                {
                    if (args[a].StartsWith("-"))
                    {
                        break;
                    }
                    values.Add(args[a]);
                }


                if (flagHandlers.TryGetValue(flag, out var handler))
                {
                    handler(values);
                }

            }
        }

        public int GetPort()
        {
            return port;
        }

        public string? GetAuthPassword()
        {
            return _authPassword;
        }

        public ServerInfoData GetServerDataInfo()
        {
            return new ServerInfoData { Role = _role, MasterReplid = _replicationId, dbFileName = _dbFileName, dir = _dir };
        }

        public MasterInfo GetMasterInfo()
        {
            return new MasterInfo { MasterHost = _masterHost, MasterPort = _masterPort, Role = _role };
        }
    }
}