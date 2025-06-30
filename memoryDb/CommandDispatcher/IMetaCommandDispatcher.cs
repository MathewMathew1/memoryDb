using System.Net.Sockets;
using RedisServer.Command.Model;

namespace RedisServer.Command.Service
{
    public interface IMetaCommandDispatcher
    {
        Task<IEnumerable<byte[]>?> DispatchCommand(ParsedCommand command, Socket socket);
        bool CommandIsInThePool(ParsedCommand command);
    }
    
}