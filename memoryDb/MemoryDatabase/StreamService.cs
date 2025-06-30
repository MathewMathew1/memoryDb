using System.Collections.Concurrent;
using RedisServer.Database.Model;
using RedisServer.RadixTree.Service;
using RedisServer.StreamServices.Service;

namespace RedisServer.Database.Service
{
    public class StreamService : IStreamService
    {
        private readonly ConcurrentDictionary<string, RadixTree<StreamEntry>> _streams = new();
        private readonly StreamWaitManager _waitManager = new();


        public ConcurrentDictionary<string, RadixTree<StreamEntry>> GetAllSnapshot()
        {
            return new ConcurrentDictionary<string, RadixTree<StreamEntry>>(_streams);
        }

        public void AddEntry(string key, StreamEntry entry)
        {
            if (!_streams.TryGetValue(key, out var tree))
            {
                tree = new RadixTree<StreamEntry>();
                _streams[key] = tree;
            }

            tree.Insert(entry.Id, entry);

            _waitManager.SignalNewData(key, entry);
        }

        public List<StreamEntry>? GetEntriesInRange(string key, string start, string? end)
        {
            _streams.TryGetValue(key, out var entries);

            if (entries == null) return new List<StreamEntry>();

            var filteredEntries = entries.Range(start, end).Select(e => e.Value).ToList();
            return filteredEntries;
        }

        public List<StreamEntry>? GetEntriesInRead(string key, string start)
        {
            _streams.TryGetValue(key, out var entries);
            if (entries == null) return new List<StreamEntry>();

            var filteredEntries = entries.Range(start, null).Select(e => e.Value).ToList();
            return filteredEntries;
        }

        public async Task<List<StreamEntry>?> GetEntriesInReadAsync(string key, string startId, TimeSpan? timeout = null)
        {
            if (startId != "$")
            {
                var entries = GetEntriesInRead(key, startId);
                if (entries?.Count > 0 || timeout == null)
                    return entries;

            }

            List<StreamEntry> listOfEntries = new List<StreamEntry>();
            var entry = await _waitManager.WaitForDataAsync(key, timeout.Value);
            if (entry != null)
            {
                listOfEntries.Add(entry);
            }


            return listOfEntries;
        }
        


        public List<StreamEntry>? GetAllEntries(string key)
        {
            _streams.TryGetValue(key, out var entries);
            if (entries == null) return new List<StreamEntry>();

            var filteredEntries = entries.Range("", null).Select(e => e.Value).ToList();
            return filteredEntries;
        }

        public bool Contains(string key)
        {
            return _streams.ContainsKey(key);
        }

        public void Delete(string key)
        {
            _streams.TryRemove(key, out _);
        }
    }
}
