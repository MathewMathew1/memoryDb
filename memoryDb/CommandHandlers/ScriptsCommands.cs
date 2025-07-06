using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.LuaManager.Service;

namespace RedisServer.CommandHandlers.Service
{
    public class ScriptCommand : ILuaCommandHandler
    {
        private readonly ILuaScriptStorage _luaScriptStorage;

        public ScriptCommand(ILuaScriptStorage luaScriptStorage)
        {
            _luaScriptStorage = luaScriptStorage;
        }

        public string CommandName => "SCRIPT";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            var subScript = command.Arguments[0];

            if (subScript.ToUpperInvariant() == "LOAD")
            {
                return LoadAScript(command, socket);
            }

            if (subScript.ToUpperInvariant() == "EXISTS")
            {
                return CheckIfScriptExist(command, socket);
            }

            if (subScript.ToUpperInvariant() == "FLUSH")
            {
                return FlushScript(command, socket);
            }


            return new[] { Encoding.UTF8.GetBytes("-ERR wrong argument for 'SCRIPT'\r\n") };
        }


        private IEnumerable<byte[]> LoadAScript(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'SCRIPT LOAD'\r\n") };

            var script = command.Arguments[1];

            var hash = _luaScriptStorage.StoreScript(script);

            return new[] { Encoding.UTF8.GetBytes($"${hash.Length}\r\n{hash}\r\n") };
        }

        private IEnumerable<byte[]> CheckIfScriptExist(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'SCRIPT EXISTS'\r\n") };

            var hashes = command.Arguments.Skip(1);

            var results = hashes.Select(hash => _luaScriptStorage.ScriptExists(hash) ? 1 : 0).ToList();

            var response = new List<byte[]>
            {
                Encoding.UTF8.GetBytes($"*{results.Count}\r\n")
            };

            foreach (var res in results)
                response.Add(Encoding.UTF8.GetBytes($":{res}\r\n"));

            return response;
        }

        private IEnumerable<byte[]> FlushScript(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'SCRIPT FLUSH'\r\n") };

            var hash = command.Arguments[1];
            var removed = _luaScriptStorage.FlushScript(hash);

            if (removed)
                return new[] { Encoding.UTF8.GetBytes("+OK\r\n") };
            else
                return new[] { Encoding.UTF8.GetBytes("-NOSCRIPT No matching script to flush\r\n") };
        }

    }


    public class FlushAllCommand : ILuaCommandHandler
    {
        private readonly ILuaScriptStorage _luaScriptStorage;

        public FlushAllCommand(ILuaScriptStorage luaScriptStorage)
        {
            _luaScriptStorage = luaScriptStorage;
        }

        public string CommandName => "FlushAll";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {


            _luaScriptStorage.FlushScripts();

            return new[] { Encoding.UTF8.GetBytes("+OK\r\n") };
        }
    }



}