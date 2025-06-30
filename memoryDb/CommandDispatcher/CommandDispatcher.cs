
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Service;

namespace RedisServer.Command.Service
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly Dictionary<string, ICommandHandler> _handlers;
        private readonly Dictionary<string, IMasterCommandHandler> _masterHandlers;

        public CommandDispatcher(IEnumerable<ICommandHandler> handlers, IEnumerable<IMasterCommandHandler> masterHandlers)
        {
            _handlers = handlers.ToDictionary(h =>{ 
                return h.CommandName.ToLowerInvariant();
            });
            _masterHandlers = masterHandlers.ToDictionary(h => h.CommandName.ToLowerInvariant());
        }

        public async Task<IEnumerable<byte[]>> DispatchCommand(ParsedCommand command, Socket socket, bool isMasterCommand = false)
        {
            if (!isMasterCommand)
            {
                if (_handlers.TryGetValue(command.Name.ToLowerInvariant(), out var handler))
                    return await handler.Handle(command, socket);
            }
            else
            {
                if (_masterHandlers.TryGetValue(command.Name.ToLowerInvariant(), out var handler))
                    return await handler.Handle(command, socket);
            }
          

            return new[] { Encoding.UTF8.GetBytes($"-ERR unknown command '{command.Name}'\r\n") };
        }

    }
}