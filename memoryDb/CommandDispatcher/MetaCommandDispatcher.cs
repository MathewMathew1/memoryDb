using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Service;

namespace RedisServer.Command.Service
{
    public class MetaCommandDispatcher : IMetaCommandDispatcher
    {
        private readonly Dictionary<string, ICommandMetaHandler> _metaHandlers;

        public MetaCommandDispatcher(IEnumerable<ICommandMetaHandler> handlers)
        {
            _metaHandlers = handlers.ToDictionary(h => h.CommandName.ToLowerInvariant());
        }

        public async Task<IEnumerable<byte[]>?> DispatchCommand(ParsedCommand command, Socket socket)
        {
            if (_metaHandlers.TryGetValue(command.Name.ToLowerInvariant(), out var handler))
            {
                return await handler.Handle(command, socket);
            }

            return null;
        }

        public  bool CommandIsInThePool(ParsedCommand command)
        {
            return _metaHandlers.TryGetValue(command.Name.ToLowerInvariant(), out var handler);
        }
    }
}
