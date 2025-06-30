
namespace RedisServer.Database.Service
{
    public interface IMemoryDatabaseRouter
    {
       string GetType(string key);
    }
}