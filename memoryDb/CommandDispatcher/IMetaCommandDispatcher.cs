using System.Net.Sockets;
using RedisServer.Command.Model;

namespace RedisServer.Command.Service
{
    public interface IMetaCommandDispatcher : ICommandDispatcher
    {
        bool CommandIsInThePool(ParsedCommand command);
    }
    
}