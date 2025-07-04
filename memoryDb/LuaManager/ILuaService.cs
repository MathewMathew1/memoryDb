using System.Net.Sockets;

namespace RedisServer.LuaManager.Service
{

    public interface  ILuaService
    {     
        object? RunScript(string script, List<string> keys, List<string> args, Socket socket);
  
    }
}
