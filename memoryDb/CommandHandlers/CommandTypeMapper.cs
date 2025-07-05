namespace RedisServer.CommandHandlers.Model
{
    public static class CommandTypeMapper
    {
        private static readonly Dictionary<string, CommandType> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["set"] = CommandType.Set,
            ["xadd"] = CommandType.XADD,
            ["lpush"] = CommandType.LPUSH,
            ["rpush"] = CommandType.RPUSH,
            ["lpop"] = CommandType.LPOP,
            ["rpop"] = CommandType.RPOP,
            ["LREM"] = CommandType.LREM,
            ["incrBy"] = CommandType.INCRBY,
            ["incr"]  = CommandType.INCR
        };

        public static bool TryParse(string commandName, out CommandType commandType)
        {
            return _map.TryGetValue(commandName, out commandType);
        }
    }
}
