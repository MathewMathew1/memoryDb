using RedisServer.ServerInfo.Model;

namespace RedisServer.ServerInfo.Service
{
    public interface IServerInfoService
    {
        public ServerInfoData GetServerDataInfo();
        public MasterInfo GetMasterInfo();
        public int GetPort();
        public string? GetAuthPassword();
    }
}