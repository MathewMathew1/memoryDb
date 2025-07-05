using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace RedisServer.LuaManager.Service
{

    public class LuaScriptStorage : ILuaScriptStorage
    {
        private readonly ConcurrentDictionary<string, string> _scripts = new ConcurrentDictionary<string, string>();

        public string StoreScript(string script)
        {
            using var sha1 = SHA1.Create();
            var hash = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(script)))
                                  .Replace("-", "").ToLowerInvariant();
            _scripts[hash] = script;
            return hash;
        }

        public string? GetScript(string hash)
        {
            return _scripts.TryGetValue(hash, out var script) ? script : null;
        }

        public bool FlushScript(string hash)
        {
            return _scripts.Remove(hash, out _);
        }
        
        public bool ScriptExists(string sha1) => _scripts.ContainsKey(sha1);

        public void FlushScripts() => _scripts.Clear();
    }


}