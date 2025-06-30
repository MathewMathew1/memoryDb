
using RedisServer.Command.Model;

namespace RedisServer.Command.Service
{
    public interface ICommandParser
    {
        public List<ParsedCommand> ParseCommands(byte[]? buffer, int bytesRead);
    };
}