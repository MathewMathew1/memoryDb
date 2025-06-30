
namespace RedisServer.Command.Model
{
    public abstract class RespValue { }

    public class RespArray : RespValue
    {
        public List<RespValue> Elements { get; }
        public RespArray(List<RespValue> elements) => Elements = elements;
    }

    public class RespBulkString : RespValue
    {
        public string Value { get; }
        public RespBulkString(string value) => Value = value;
    }

    public class RespSimpleString : RespValue
    {
        public string Value { get; }
        public RespSimpleString(string value) => Value = value;
    }

    public class RespError : RespValue
    {
        public string Message { get; }
        public RespError(string message) => Message = message;
    }
}
