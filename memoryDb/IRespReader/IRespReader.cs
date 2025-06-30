using RedisServer.Command.Model;

namespace RedisServer.RespMessage.Service
{
    public interface IRespMessageReader
    {
        bool TryReadNextMessage(Stream stream, out ParsedCommand? command);
    }
}
