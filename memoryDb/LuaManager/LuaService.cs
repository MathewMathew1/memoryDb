using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLua;
using RedisServer.Command.Model;
using RedisServer.Command.Service;

namespace RedisServer.LuaManager.Service
{

    public class LuaService : ILuaService
    {
        private readonly Lua _lua;
        private readonly ILogger<LuaService> _logger;
        private readonly CommandDispatcher _commandDispatcher;
        private Socket? _currentSocket;

        public LuaService(ILogger<LuaService> logger, CommandDispatcher commandDispatcher)
        {
            _lua = new Lua();
            _lua["os"] = null;
            _lua["io"] = null;
            _lua["debug"] = null;
            _lua["package"] = null;
            _lua["dofile"] = null;
            _lua["loadfile"] = null;
            _lua["require"] = null;
            _logger = logger;
            _lua.NewTable("redis");
            _lua.RegisterFunction("redis.call", this, GetType().GetMethod(nameof(RedisCall), BindingFlags.Instance | BindingFlags.NonPublic));

            _commandDispatcher = commandDispatcher;
        }

        public object? RunScript(string script, List<string> keys, List<string> args, Socket socket)
        {
            _currentSocket = socket;

            _lua.NewTable("KEYS");
            var keysTable = (LuaTable)_lua["KEYS"];
            for (int i = 0; i < keys.Count; i++)
                keysTable[i + 1] = keys[i];
            _lua["KEYS"] = keysTable;

            _lua.NewTable("ARGV");
            var argsTable = (LuaTable)_lua["ARGV"];
            for (int i = 0; i < args.Count; i++)
                argsTable[i + 1] = args[i];
            _lua["ARGV"] = argsTable;

            try
            {
                var result = _lua.DoString(script);
                _logger.LogError($"Lua Script: {result?.FirstOrDefault()}");
                return result?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lua Error: {ex.Message}");
                return null;
            }
            finally
            {
                _currentSocket = null;
            }
        }


        private object? RedisCall(params object[] args)
        {
            if (args.Length == 0 || args[0] is not string commandName)
                throw new Exception("Missing or invalid command name in redis.call");

            List<string> arguments = new();
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] is string arg)
                    arguments.Add(arg);
            }

            var parsedCommand = new ParsedCommand { Name = commandName, Arguments = arguments };

            var task = _commandDispatcher.DispatchCommand(parsedCommand, _currentSocket);
            task.Wait();
            var resultBytes = task.Result;

            var first = resultBytes.FirstOrDefault();
            if (first == null) return null;

            var resp = Encoding.UTF8.GetString(first);

            if (resp.StartsWith("$"))
            {
                int i = resp.IndexOf("\r\n", StringComparison.Ordinal);
                if (i >= 0 && i + 2 < resp.Length)
                    return resp.Substring(i + 2).TrimEnd('\r', '\n');
            }
            else if (resp.StartsWith(":"))
            {
                return int.Parse(resp.Substring(1).TrimEnd('\r', '\n'));
            }
            else if (resp.StartsWith("+"))
            {
                return resp.Substring(1).TrimEnd('\r', '\n');
            }
            else
            {
                return resp;
            }
            return null;
        }


    }
}
