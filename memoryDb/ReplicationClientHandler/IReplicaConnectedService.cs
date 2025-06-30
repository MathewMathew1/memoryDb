using System.Net.Sockets;

namespace RedisServer.Replication.Service
{
    public interface IReplicaSocketService
    {
        void AddReplica(Socket socket);
        void RemoveReplica(Socket socket);
        void Broadcast(byte[] message);
        int GetAmountOfReplicas();
        public int ReplicasInSync { get; }
        void AddReplicaToSyncIfOffsetCorrect(Socket socket, long offset);
    }
}
