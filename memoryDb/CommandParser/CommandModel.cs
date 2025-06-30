namespace RedisServer.Command.Model
{
    public class ParsedCommand
    {
        public required string Name { get; set; }
        public required List<string> Arguments { get; set; }
        public int BytesConsumed { get; set; }
    }
}