using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using RedisServer.Command.Model;
using RedisServer.Command.Service;
using RedisServer.Connection.Service;
using RedisServer.ServerInfo.Service;

namespace RedisServer.Listener
{
    public class RedisServerListener
    {
        private readonly TcpListener _tcpServer;
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger<RedisServerListener> _logger;

        private readonly ICommandParser _commandParser;
    
        private readonly IServerInfoService _serverInfoService;
        private readonly IMetaCommandDispatcher _metaCommandDispatcher;
        private static readonly HashSet<string> SafeCommands = new() { "AUTH", "PING" };
        private readonly List<ICommandDispatcher> _commandDispatchers = new List<ICommandDispatcher>();

        public RedisServerListener(TcpListener tcpServer, ConnectionManager connectionManager, ILogger<RedisServerListener> logger, CommandDispatcher commandDispatcher,
        ICommandParser commandParser, IServerInfoService serverInfoService, IMetaCommandDispatcher metaCommandDispatcher,
        LuaCommandDispatcher luaCommandDispatcher)
        {
            _tcpServer = tcpServer;
            _connectionManager = connectionManager;
            _logger = logger;
            _commandParser = commandParser;
            _serverInfoService = serverInfoService;
            _metaCommandDispatcher = metaCommandDispatcher;

            _commandDispatchers.Add(metaCommandDispatcher);
            _commandDispatchers.Add(commandDispatcher);
            _commandDispatchers.Add(luaCommandDispatcher);

            _tcpServer.Start();
        }

        public async Task StartAcceptLoopAsync()
        {

            while (true)
            {
                Socket clientSocket = await _tcpServer.AcceptSocketAsync();

                var password = _serverInfoService.GetAuthPassword();
                _connectionManager.AddSocket(clientSocket, password != null ? false : true);

                _ = Task.Run(() => HandleClientAsync(clientSocket));
            }
        }

        public async Task HandleClientAsync(Socket socket)
        {
            var buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await socket.ReceiveAsync(buffer);
                    if (bytesRead == 0) break;

                    var commands = _commandParser.ParseCommands(buffer, bytesRead);

                    if (commands.Count == 0) break;
                    foreach (var command in commands)
                    {
                        var connection = _connectionManager.GetSocketConnection(socket)!;

                        if (connection.isLocked == false || _metaCommandDispatcher.CommandIsInThePool(command))
                        {
                            var commandName = command.Name.ToUpperInvariant();
                            if (!connection.isAuth && !SafeCommands.Contains(commandName))
                            {
                                var errorMessage = $"-NOAUTH Authentication required.\r\n";
                                await socket.SendAsync(Encoding.UTF8.GetBytes(errorMessage), SocketFlags.None);

                            }
                            else
                            {
                                await ExecuteCommand(command, socket);
                            }

                        }
                        else
                        {
                            _connectionManager.AddCommandToQueue(socket, command);
                            await socket.SendAsync(Encoding.UTF8.GetBytes("+QUEUED\r\n"));
                        }

                    }


                }
            }
            catch (SocketException e)
            {
                _logger.LogError($"{e}");
            }
            finally
            {
                _connectionManager.Disconnect(socket);
                socket.Close();

            }
        }

        private async Task ExecuteCommand(ParsedCommand command, Socket socket)
        {
         

            try
            {
                IEnumerable<byte[]>? values = null;
                foreach (var dispatcher in _commandDispatchers)
                {
                    values = await dispatcher.DispatchCommand(command, socket);

                    if (values != null) break;
                }

                if (values != null)
                {
                    foreach (var value in values)
                    {
                        await socket.SendAsync(value, SocketFlags.None);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"error {e}");
                var errorMessage = $"-ERR unexpected error\r\n";
                await socket.SendAsync(Encoding.UTF8.GetBytes(errorMessage), SocketFlags.None);
            }
        }

    }
}
