using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.Connection.Service;
using RedisServer.ServerInfo.Service;


namespace RedisServer.CommandHandlers.Service
{
    public class AuthCommand : ICommandHandler
    {
        private readonly IServerInfoService _serverInfoService;
        private readonly ConnectionManager _connectionManager;

        public AuthCommand(IServerInfoService serverInfoService, ConnectionManager connectionManager)
        {
            _serverInfoService = serverInfoService;
            _connectionManager = connectionManager;
        }

        public string CommandName => "Auth";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count == 0)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'auth'\r\n") };
            }


            var password = command.Arguments[0];
            var isPasswordCorrect = _serverInfoService.GetAuthPassword() == password;


            if (isPasswordCorrect)
            {
                _connectionManager.ChangeAuthSocket(socket, true);
                return new[] { Encoding.UTF8.GetBytes("+OK\r\n") };
            }
                

            return new[] { Encoding.UTF8.GetBytes("-ERR invalid password\r\n") };
        }
    }
}

