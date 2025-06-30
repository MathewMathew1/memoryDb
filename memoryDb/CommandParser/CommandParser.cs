using System.Text;
using Microsoft.Extensions.Logging;
using RedisServer.Command.Model;

namespace RedisServer.Command.Service
{
    public class CommandParser : ICommandParser
    {
        ILogger<CommandParser> _logger;

        public CommandParser(ILogger<CommandParser> logger)
        {
            _logger = logger;
        }

        public List<ParsedCommand> ParseCommands(byte[] buffer, int bytesRead)
        {
            var commands = new List<ParsedCommand>();
            int offset = 0;

            while (offset < bytesRead)
            {
                if (buffer[offset] != (byte)'*') 
                {
                    offset++; 
                    continue;
                }

                int commandStartOffset = offset;

                int lineEnd = FindLineEnd(buffer, offset, bytesRead);
                if (lineEnd == -1) break; 
                int argCount = int.Parse(Encoding.ASCII.GetString(buffer, offset + 1, lineEnd - offset - 1));
                offset = lineEnd + 2; 

                var args = new List<string>();
                for (int i = 0; i < argCount; i++)
                {
                    if (offset >= bytesRead || buffer[offset] != (byte)'$') return commands;

                    lineEnd = FindLineEnd(buffer, offset, bytesRead);
                    if (lineEnd == -1) break;
                    int len = int.Parse(Encoding.ASCII.GetString(buffer, offset + 1, lineEnd - offset - 1));
                    offset = lineEnd + 2;

                    if (offset + len + 2 > bytesRead) break; 
                    string arg = Encoding.UTF8.GetString(buffer, offset, len);
                    args.Add(arg);

                    offset += len + 2; 
                }

                if (args.Count > 0)
                {
                    commands.Add(new ParsedCommand
                    {
                        Name = args[0],
                        Arguments = args.Skip(1).ToList(),
                         BytesConsumed = offset - commandStartOffset
                    });
                }
            }

            return commands;
        }

        private int FindLineEnd(byte[] buffer, int start, int max)
        {
            for (int i = start; i < max - 1; i++)
            {
                if (buffer[i] == '\r' && buffer[i + 1] == '\n')
                    return i;
            }
            return -1;
        }

    }

}