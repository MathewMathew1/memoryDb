/*using System.Text;
using Microsoft.Extensions.Logging;
using RedisServer.Command.Model;
using RedisServer.Command.Service;

namespace RedisServer.RespMessage.Service
{
    public sealed class RespMessageReader : IRespMessageReader
    {
        private readonly ICommandParser _commandParser;
        private readonly ILogger<RespMessageReader> _logger;

        public RespMessageReader(ICommandParser commandParser, ILogger<RespMessageReader> logger)
        {
            _commandParser = commandParser;
            _logger = logger;
        }

        public bool TryReadNextMessage(Stream stream, out ParsedCommand? command)
        {
            command = null;
            var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

            int prefix = stream.ReadByte();
            if (prefix == -1) return false;

            switch ((char)prefix)
            {
                case '+':
                    string line = reader.ReadLine();
                    _logger.LogInformation("Received simple string: +{0}", line);
                    return true;

                case '$':
                    string lenLine = reader.ReadLine();
                    if (!int.TryParse(lenLine, out int blobLength))
                    {
                        _logger.LogWarning("Invalid bulk length: {0}", lenLine);
                        return false;
                    }

                    var blob = new byte[blobLength];
                    int read = stream.Read(blob, 0, blobLength);
                    if (read != blobLength)
                    {
                        _logger.LogWarning("Incomplete bulk read. Expected {0}, got {1}", blobLength, read);
                        return false;
                    }

                    _logger.LogInformation("Received bulk binary block of {0} bytes", blobLength);

                    stream.ReadByte(); // Expect '\r'
                    stream.ReadByte(); // Expect '\n'

                    return true;

                case '*':
                    _logger.LogInformation($"I lost all hopes and dreams {stream.ToString()}");
                    stream.Position--;
                    command = _commandParser.TryParseNextCommand(stream);
                    return command != null;

                default:
                    _logger.LogWarning("Unhandled RESP type: {0}", (char)prefix);
                    return false;
            }
        }
    }
}*/
