
using RedisServer.Database.Model;

namespace RedisServer.Database.Service
{
    public interface IStringService
    {

        void Set(string key, string value, SetKeyParameters parameters);
        string? Get(string key);
        void CleanDataSet();
        bool Contains(string key);
        int? Increase(string key);
        void Delete(string key);
        object SyncRoot { get; }
        Dictionary<string, ValueInMemory> GetAllSnapshot();

    }

}