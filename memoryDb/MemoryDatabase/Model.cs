using RedisServer.RadixTree.Service;

namespace RedisServer.Database.Model
{
    public class SetKeyParameters
    {
        public double? expirationTime { get; set; }
    }

    public class ValueInMemory
    {
        public required string value { get; set; }
        public DateTime? expirationDate { get; set; }
    }

    public class StreamEntry
    {
        public required string Id { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new();
    }

    class RedisStream
    {
        RadixTree<StreamEntry> Entries;  
    }
}