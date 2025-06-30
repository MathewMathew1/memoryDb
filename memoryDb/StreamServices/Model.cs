

namespace RedisServer.StreamServices.Model
{
    public class ParsedSteamId
    {
        public required long Milliseconds { get; set; }
        public required string Sequence { get; set; }

    }
    
     public class ParsedSteamIdWithNumber
    {
        public required long Milliseconds { get; set;}
        public required int Sequence { get; set; }
        
    }
}