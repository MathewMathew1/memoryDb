using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using RedisServer.Command.Model;
using RedisServer.Replication.Service;

namespace RedisServer.CommandHandlers.Service
{
    public class WaitCommand : ICommandHandler
    {
        private readonly IReplicaSocketService _replicaSocketService;
        private readonly ILogger<WaitCommand> _logger;

        public WaitCommand(IReplicaSocketService replicaSocketService, ILogger<WaitCommand> logger)
        {
            _replicaSocketService = replicaSocketService;
            _logger = logger;
        }

        public string CommandName => "wait";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            int minimumNoReplicas = Int32.Parse(command.Arguments[0]);
            int timeoutFromCommand = Int32.Parse(command.Arguments[1]);

            var replicas = _replicaSocketService.GetAmountOfReplicas();
            if (_replicaSocketService.ReplicasInSync != _replicaSocketService.GetAmountOfReplicas())
            {
                replicas = await WaitForReplicasAsync(minimumNoReplicas, 0, timeoutFromCommand);
          
            }

            string response = $":{replicas}\r\n";

            return new[] { Encoding.UTF8.GetBytes(response) };
        }

        private static readonly byte[] GetAckCommand = Encoding.UTF8.GetBytes(
            "*3\r\n$8\r\nREPLCONF\r\n$6\r\nGETACK\r\n$1\r\n*\r\n"
        );

        private async Task<int> WaitForReplicasAsync(int expected, long targetOffset, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            _replicaSocketService.Broadcast(GetAckCommand);

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (_replicaSocketService.ReplicasInSync >= expected)
                    return _replicaSocketService.ReplicasInSync;

                await Task.Delay(10);
            }

            return _replicaSocketService.ReplicasInSync;
        }
    }

}