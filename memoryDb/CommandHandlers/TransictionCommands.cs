using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.Connection.Service;
using RedisServer.Transaction.Service;

namespace RedisServer.CommandHandlers.Service
{
    public class MultiCommand : ICommandHandler
    {
        private readonly ConnectionManager _connectionManager;
        public string CommandName => "Multi";


        public MultiCommand(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;

        }

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            _connectionManager.ChangeLockSocket(socket, true);

            return new[] { Encoding.UTF8.GetBytes("+OK\r\n") };
        }
    }

    public class ExecCommand : ICommandMetaHandler
    {
        private readonly ConnectionManager _connectionManager;

        private readonly ITransactionExecutor _executor;

        public string CommandName => "Exec";

        public ExecCommand(ConnectionManager connectionManager, ITransactionExecutor executor)
        {
            _connectionManager = connectionManager;
            _executor = executor;
        }

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            var connection = _connectionManager.GetSocketConnection(socket)!;

            if (!connection.isLocked)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR EXEC without MULTI\r\n") };
            }

            var results = await _executor.ExecuteQueuedCommandsAsync(socket, connection);

            var response = new List<byte[]>
        {
            Encoding.UTF8.GetBytes($"*{results.Count}\r\n")
        };

            foreach (var cmdResult in results)
            {
                foreach (var line in cmdResult)
                {
                    response.Add(line);
                }
            }

            return response;
        }
    }
    
    public class DiscardCommand : ICommandMetaHandler
    {
        private readonly ConnectionManager _connectionManager;


        public string CommandName => "DISCARD";

        public DiscardCommand(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            var connection = _connectionManager.GetSocketConnection(socket)!;

            if (!connection.isLocked)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR DISCARD without MULTI\r\n") };
            }

            _connectionManager.AbortTransaction(socket);

            return new[] { Encoding.UTF8.GetBytes("+OK\r\n") };
        }
    }



}