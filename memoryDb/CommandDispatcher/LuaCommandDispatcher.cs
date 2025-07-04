
using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Service;

namespace RedisServer.Command.Service
{
    public class LuaCommandDispatcher : ICommandDispatcher
    {
        private readonly Dictionary<string, ILuaCommandHandler> _handlers;

        public LuaCommandDispatcher(IEnumerable<ILuaCommandHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h =>
            {
                return h.CommandName.ToLowerInvariant();
            });

        }

        public async Task<IEnumerable<byte[]>?> DispatchCommand(ParsedCommand command, Socket socket)
        {

            if (_handlers.TryGetValue(command.Name.ToLowerInvariant(), out var handler))
                return await handler.Handle(command, socket);


            return null;
        }

    }
}