using System.Net.Sockets;
using RedisServer.Connection.Model;

namespace RedisServer.Transaction.Service
{
    public interface ITransactionExecutor
    {
        Task<List<IEnumerable<byte[]>>> ExecuteQueuedCommandsAsync(Socket socket, ConnectionState connection);
    }
}