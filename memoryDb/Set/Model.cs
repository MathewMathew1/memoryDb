namespace RedisServer.Database.Model
{
    public class SkiplistData
    {
        public required string Member { get; set; }
        public required double Score { get; set;}
    }
}