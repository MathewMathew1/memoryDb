using System.Net.Sockets;
using System.Text;
using NLua;
using RedisServer.Command.Model;
using RedisServer.LuaManager.Service;

namespace RedisServer.CommandHandlers.Service
{
    public class EvalCommand : ILuaCommandHandler
    {
        private readonly ILuaService _luaManager;

        public EvalCommand(ILuaService luaService)
        {
            _luaManager = luaService;
        }

        public string CommandName => "Eval";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            var script = command.Arguments[0];
            int numKeys = int.Parse(command.Arguments[1]);
            var keys = command.Arguments.Skip(2).Take(numKeys).ToList();
            var args = command.Arguments.Skip(2 + numKeys).ToList();

            var result = _luaManager.RunScript(script, keys, args, socket);

            return new[] { SerializeResp(result) };
        }

        private byte[] SerializeResp(object? obj)
        {
            if (obj is null)
                return Encoding.UTF8.GetBytes("$-1\r\n");
            if (obj is string s)
                return Encoding.UTF8.GetBytes($"${s.Length}\r\n{s}\r\n");
            if (obj is int i)
                return Encoding.UTF8.GetBytes($":{i}\r\n");
            if (obj is IEnumerable<object> list)
            {
                var items = list.Cast<object>().ToList();
                var builder = new StringBuilder($"*{items.Count}\r\n");
                foreach (var item in items)
                    builder.Append(Encoding.UTF8.GetString(SerializeResp(item)));
                return Encoding.UTF8.GetBytes(builder.ToString());
            }

            var fallback = obj.ToString() ?? "";
            return Encoding.UTF8.GetBytes($"${fallback.Length}\r\n{fallback}\r\n");
        }


    }


}