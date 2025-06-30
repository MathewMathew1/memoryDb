

namespace RedisServer.Replication.Service
{
    public interface IReplicationHandshakeClient
    {
        public Task PerformHandshakeAsync(string host, int port, int localPort);    
    }
}