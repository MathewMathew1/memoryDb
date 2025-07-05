namespace RedisServer.LuaManager.Service
{

    public interface ILuaScriptStorage
    {
        string StoreScript(string script);
        string? GetScript(string hash);
        bool ScriptExists(string sha1);
        void FlushScripts();
        bool FlushScript(string hash);
    }
}