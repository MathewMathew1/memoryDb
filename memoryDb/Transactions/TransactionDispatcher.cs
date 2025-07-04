
using System.Net.Sockets;
using RedisServer.Command.Service;
using RedisServer.Connection.Model;
using RedisServer.Connection.Service;

namespace RedisServer.Transaction.Service
{
    public class TransactionExecutor : ITransactionExecutor
    {
        private readonly CommandDispatcher _dispatcher;
        private readonly ConnectionManager _connectionManager;

        public TransactionExecutor(CommandDispatcher dispatcher, ConnectionManager connectionManager)
        {
            _dispatcher = dispatcher;
            _connectionManager = connectionManager;
        }

        public async Task<List<IEnumerable<byte[]>>> ExecuteQueuedCommandsAsync(Socket socket, ConnectionState connection)
        {
            var result = new List<IEnumerable<byte[]>>();

            if (connection == null)
                return result;

            _connectionManager.ChangeLockSocket(socket, false);

            while (connection.CommandsInQueue.Count > 0)
            {
                
                var command = connection.CommandsInQueue.Dequeue();
 
                var response = await _dispatcher.DispatchCommand(command, socket);
                result.Add(response);
            }

            return result;
        }
    }

}
