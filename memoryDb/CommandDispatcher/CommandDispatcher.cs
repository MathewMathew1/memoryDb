
using System.Net.Sockets;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Model;
using RedisServer.CommandHandlers.Service;
using RedisServer.Replication.Service;
using RedisServer.ServerInfo.Service;
using RedisServer.Util.Serialization;

namespace RedisServer.Command.Service
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly Dictionary<string, ICommandHandler> _handlers;
        private readonly IReplicaSocketService _replicaSocketService;
        private readonly IServerInfoService _serverInfoService;

        public CommandDispatcher(IEnumerable<ICommandHandler> handlers, IReplicaSocketService replicaSocketService, IServerInfoService serverInfoService)
        {
            _replicaSocketService = replicaSocketService;
            _serverInfoService = serverInfoService;

            _handlers = handlers.ToDictionary(h =>
            {
                return h.CommandName.ToLowerInvariant();
            });
        }

        public async Task<IEnumerable<byte[]>?> DispatchCommand(ParsedCommand command, Socket socket)
        {
            if (CommandTypeMapper.TryParse(command.Name, out var type) &&
            _serverInfoService.GetServerDataInfo().Role == ServerInfo.Model.Role.MASTER)
            {
                var serializedCommand = Serialization.SerializeCommandToRESP(command);

                _replicaSocketService.Broadcast(serializedCommand);
            }

            if (_handlers.TryGetValue(command.Name.ToLowerInvariant(), out var handler))
                return await handler.Handle(command, socket);

            return null; ;
        }

    }
}