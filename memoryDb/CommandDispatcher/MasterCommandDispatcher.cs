using System.Net.Sockets;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Service;

namespace RedisServer.Command.Service
{
    public class MasterCommandDispatcher : ICommandDispatcher
    {
        private readonly Dictionary<string, IMasterCommandHandler> _masterHandlers;

        public MasterCommandDispatcher(IEnumerable<IMasterCommandHandler> masterHandlers)
        {

            _masterHandlers = masterHandlers.ToDictionary(h => h.CommandName.ToLowerInvariant());
        }

        public async Task<IEnumerable<byte[]>?> DispatchCommand(ParsedCommand command, Socket socket)
        {

            if (_masterHandlers.TryGetValue(command.Name.ToLowerInvariant(), out var handler))
                return await handler.Handle(command, socket);



            return null; ;
        }

    }
}