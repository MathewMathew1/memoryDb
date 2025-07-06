using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.RdbFile.Service;
using RedisServer.ServerInfo.Service;

namespace RedisServer.CommandHandlers.Service
{
    public class ConfigCommand : ICommandHandler
    {
        private readonly IServerInfoService _serverInfoService;
        public ConfigCommand(IServerInfoService serverInfoService)
        {
            _serverInfoService = serverInfoService;
        }

        public string CommandName => "CONFIG";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'config'\r\n") };

            Dictionary<string, string> keys = new Dictionary<string, string>();
            var serverInfo = _serverInfoService.GetServerDataInfo();

            var flagHandlers = new Dictionary<string, Action>
            {
                ["dir"] = () =>
                {
                    keys.Add("dir", serverInfo.dir);
                },
                ["dbfilename"] = () =>
                {
                    keys.Add("dbfilename", serverInfo.dbFileName);
                }
            };

            for (int i = 1; i < command.Arguments.Count; i += 1)
            {
                var flag = command.Arguments[i].ToLowerInvariant();

                if (flagHandlers.TryGetValue(flag, out var handler))
                {
                    handler();
                }

            }

            if (keys.Count == 0)
                return new[] { Encoding.UTF8.GetBytes("*0\r\n") };

            var responseBuilder = new StringBuilder();
            responseBuilder.Append($"*{keys.Count * 2}\r\n");

            foreach (var pair in keys)
            {
                var key = pair.Key;
                var value = pair.Value;

                responseBuilder.Append($"${key.Length}\r\n{key}\r\n");
                responseBuilder.Append($"${value.Length}\r\n{value}\r\n");
            }

            return new[] { Encoding.UTF8.GetBytes(responseBuilder.ToString()) };
        }
    }

    public class KeysCommand : ICommandHandler
    {
        private readonly IServerInfoService _serverInfoService;
        private readonly IRdbFileService _rdbFileService;

        public KeysCommand(IServerInfoService serverInfoService, IRdbFileService rdbFileService)
        {
            _serverInfoService = serverInfoService;
            _rdbFileService = rdbFileService;
        }

        public string CommandName => "KEYS";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            List<string> keys = _rdbFileService.GetKeys(command.Arguments[0]);

            StringBuilder response  = new StringBuilder();
          
            response.Append($"*{keys.Count}\r\n");

            foreach (var key in keys)
            {
                response.Append($"${key.Length}\r\n{key}\r\n");
            }

            return new[] { Encoding.UTF8.GetBytes(response.ToString()) };
        }
    }




}