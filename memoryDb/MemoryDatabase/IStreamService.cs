using System.Collections.Concurrent;
using RedisServer.Database.Model;
using RedisServer.RadixTree.Service;

namespace RedisServer.Database.Service
{
    public interface IStreamService
    {
        void AddEntry(string key, StreamEntry entry);
        ConcurrentDictionary<string, RadixTree<StreamEntry>> GetAllSnapshot();
        List<StreamEntry>? GetEntriesInRange(string key, string start, string? end);
        List<StreamEntry>? GetEntriesInRead(string key, string start);   
        List<StreamEntry>? GetAllEntries(string key);
        Task<List<StreamEntry>?> GetEntriesInReadAsync(string key, string startId, TimeSpan? timeout = null);
        bool Contains(string key);
        void Delete(string key);    
    }
}