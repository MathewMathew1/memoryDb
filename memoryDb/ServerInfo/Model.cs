namespace RedisServer.ServerInfo.Model
{
    public enum Role
    {
        MASTER,
        SLAVE
    }

    public class ServerInfoData
    {
        public required Role Role { get; set; }
        public required string MasterReplid { get; set; }
        public long MasterReplOffset { get; set; } = 0;
        public required string dir { get; set; }
        public required string dbFileName { get; set; }
    }

    public class MasterInfo
    {
        public string? MasterHost { get; set; }
        public int? MasterPort { get; set; }
        public required Role Role { get; set; }
    }
}