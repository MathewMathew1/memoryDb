using System.Net.Sockets;
using RedisServer.Command.Model;

namespace RedisServer.Command.Service
{
    public interface ICommandDispatcher
    {
        public Task<IEnumerable<byte[]>?> DispatchCommand(ParsedCommand command, Socket socket);
    }
    
}