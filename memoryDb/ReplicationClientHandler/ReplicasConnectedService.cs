using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace RedisServer.Replication.Service
{
    public class ReplicaSocketService : IReplicaSocketService
    {
        private readonly ConcurrentDictionary<Socket, long> _replicaSockets = new();

        private readonly ILogger<ReplicaSocketService> _logger;
        public int ReplicasInSync { get; private set; } = 0;


        public ReplicaSocketService(ILogger<ReplicaSocketService> logger)
        {
            _logger = logger;
        }

        public void AddReplica(Socket socket)
        {
            _replicaSockets.TryAdd(socket, 0);
            ReplicasInSync += 1;
        }

        public void RemoveReplica(Socket socket)
        {
            _replicaSockets.TryRemove(socket, out _);
            ReplicasInSync = Math.Min(ReplicasInSync - 1, 0);
        }

        public int GetAmountOfReplicas()
        {
            return _replicaSockets.Count;
        }

        public void AddReplicaToSyncIfOffsetCorrect(Socket socket, long offset)
        {
            if (_replicaSockets.TryGetValue(socket, out long storedOffset))
            {
                _logger.LogInformation($"{storedOffset} < {offset}");
                if (storedOffset - 37 <= offset)
                {
                    ReplicasInSync++;
                }
            }
        }


        public void Broadcast(byte[] data)
        {
            ReplicasInSync = 0;

            long dataLength = data.Length;

            foreach (var socket in _replicaSockets.Keys)
            {
           
                try
                {
                    socket.Send(data);

                    _replicaSockets.AddOrUpdate(
                        socket,
                        dataLength,
                        (key, oldOffset) => oldOffset + dataLength
                    );
                }
                catch (SocketException e)
                {
                    _logger.LogCritical($"socket info {e}");
                    _replicaSockets.TryRemove(socket, out _);
                }
            }
        }

    }
}
