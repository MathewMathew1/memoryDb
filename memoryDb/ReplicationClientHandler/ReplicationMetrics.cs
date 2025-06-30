namespace RedisServer.Replication.Service
{
    public class ReplicationMetrics : IReplicationMetrics
    {
        public long BytesReadFromMaster { get; set; }
    }
}