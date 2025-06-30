using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.Replication.Service;

namespace RedisServer.CommandHandlers.Service
{
    public class AckCommand : IMasterCommandHandler
    {
        private readonly IReplicationMetrics _replicationMetrics;
        public AckCommand(IReplicationMetrics replicationMetrics)
        {
            _replicationMetrics = replicationMetrics;
        }

        public string CommandName => "replconf";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            string response = $"*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${_replicationMetrics.BytesReadFromMaster.ToString().Length}\r\n{_replicationMetrics.BytesReadFromMaster}\r\n";
            return new[] { Encoding.UTF8.GetBytes(response) };
        }
    }

    public class ReplConfCommandHandler : ICommandHandler
    {
        private readonly IReplicaSocketService _replicaSocketService;

        public ReplConfCommandHandler(IReplicaSocketService replicaSocketService)
        {
            _replicaSocketService = replicaSocketService;
        }

        public string CommandName => "replconf";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            var response = "+OK\r\n";
            if (command.Arguments.Count >= 2 &&
                command.Arguments[0].ToLower() == "ack" &&
                long.TryParse(command.Arguments[1], out long offset))
            {
                response = string.Empty;
                _replicaSocketService.AddReplicaToSyncIfOffsetCorrect(socket, offset);
            }

             return new[] { Encoding.UTF8.GetBytes(response) };
        }
    }

}