namespace RedisServer.Replication.Service
{
    public interface IReplicationMetrics
    {
        long BytesReadFromMaster { get; set; }
    }
}