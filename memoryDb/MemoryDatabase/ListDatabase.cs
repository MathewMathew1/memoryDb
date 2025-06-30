using System.Collections.Concurrent;
using RedisServer.StreamServices.Service;

namespace RedisServer.Database.Service
{
    public class ListDatabase : IListDatabase
    {
        private readonly ConcurrentDictionary<string, ConcurrentLinkedList> _lists = new();
        private readonly StreamWaitManager _waitManager = new();

        public ConcurrentDictionary<string, ConcurrentLinkedList> GetSnapshot()
        {
            return new ConcurrentDictionary<string, ConcurrentLinkedList>(_lists);
        }

        public void AddLeft(string key, string value)
        {
            var list = _lists.GetOrAdd(key, _ => new ConcurrentLinkedList());
            list.PushLeft(value);
        }

        public void AddRight(string key, string value)
        {
            var list = _lists.GetOrAdd(key, _ => new ConcurrentLinkedList());
            list.PushRight(value);
        }

        public string? PopLeft(string key)
        {
            var list = _lists.GetOrAdd(key, _ => new ConcurrentLinkedList());
            return list.PopLeft();
        }

        public string? PopRight(string key)
        {
            var list = _lists.GetOrAdd(key, _ => new ConcurrentLinkedList());
            return list.PopRight();
        }

        public List<string> GetRange(string key, int start, int end)
        {
            var list = _lists.GetOrAdd(key, _ => new ConcurrentLinkedList());
            return list.RangeLeft(start, end);
        }

        public int GetNumberOfElements(string key)
        {
            _lists.TryGetValue(key, out var list);
            if (list == null) return 0;

            return list.GetNumberOfElements();
        }

        public int RemoveValue(string key, int count, string value)
        {
            _lists.TryGetValue(key, out var list);
            if (list == null) return 0;

            return list.RemoveValue(value, count);
        }

   
    }

}