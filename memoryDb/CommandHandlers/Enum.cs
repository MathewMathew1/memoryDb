namespace RedisServer.CommandHandlers.Model
{
    public enum CommandType
    {
        Set,
        XADD,
        LPUSH,
        RPUSH,
        LPOP,
        RPOP,
        LREM,
        INCRBY,
        INCR,
        ZADD,
        ZINCRBY,
        ZREM,
        ZREMRANGEBYSCORE,
        ZREMRANGEBYRANK
    }
}