
using System.Text;
using RedisServer.Command.Model;

namespace RedisServer.Util.Serialization
{
    public static class Serialization
    {
        public static byte[] SerializeCommandToRESP(ParsedCommand command)
        {
            var parts = new List<byte[]>
    {
        Encoding.UTF8.GetBytes($"*{command.Arguments.Count + 1}\r\n"),
        Encoding.UTF8.GetBytes($"${command.Name.Length}\r\n{command.Name}\r\n")
    };

            foreach (var arg in command.Arguments)
            {
                parts.Add(Encoding.UTF8.GetBytes($"${arg.Length}\r\n{arg}\r\n"));
            }

            return parts.SelectMany(b => b).ToArray();
        }
    }
}
