using System.Net.Sockets;
using System.Reflection;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.ServerInfo.Service;

namespace RedisServer.CommandHandlers.Service
{
    public class PingCommand : ICommandHandler
    {
        public string CommandName => "ping";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            return new[] {Encoding.UTF8.GetBytes("+PONG\r\n")};
        }
    }

    public class EchoCommand : ICommandHandler
    {
        public string CommandName => "echo";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count == 0)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'echo'\r\n") };

            string value = command.Arguments[0];
            return new[] { Encoding.UTF8.GetBytes($"${value.Length}\r\n{value}\r\n") };
        }
    }

    public class InfoCommand : ICommandHandler
    {
        public string CommandName => "info";
        private readonly IServerInfoService _serverInfoService;

        public InfoCommand(IServerInfoService serverInfoService)
        {
            _serverInfoService = serverInfoService;
        }

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            var serverInfo = _serverInfoService.GetServerDataInfo();

            var sb = new StringBuilder();
            foreach (PropertyInfo prop in serverInfo.GetType().GetProperties())
            {
                var name = StringConverter.ToSnakeCase(prop.Name);
                var value = prop.GetValue(serverInfo) ?? "";
                sb.AppendLine($"{name}:{value.ToString().ToLowerInvariant()}");
            }
            var result = sb.ToString();
            var response = $"${Encoding.UTF8.GetByteCount(result)}\r\n{result}\r\n";
            return new[] { Encoding.UTF8.GetBytes(response) };
        }
    }
}
