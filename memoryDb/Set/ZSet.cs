using System.Collections.Concurrent;

namespace RedisServer.Database.Model
{
    public class ZSet
    {
        private readonly ConcurrentDictionary<string, double> _keyValuePairs = new ConcurrentDictionary<string, double>();
        private readonly Skiplist _skiplist = new Skiplist();

        private readonly object _lock = new();


        public void AddOrUpdate(string member, double value)
        {
            lock (_lock)
            {
                if (_keyValuePairs.TryGetValue(member, out var oldValue))
                {
                    if (oldValue == value) return;
                    _skiplist.Erase(oldValue, member);
                }

                _keyValuePairs[member] = value;
                _skiplist.Add(member, value);
            }
        }


        public void Delete(string member)
        {
            lock (_lock)
            {

                if (_keyValuePairs.Remove(member, out var oldValue)) ;

            }
        }


        public double IncreaseBy(string member, double increaseBy)
        {
            lock (_lock)
            {
                var newValue = increaseBy;
                if (_keyValuePairs.TryGetValue(member, out var oldValue))
                {
                    _skiplist.Erase(oldValue, member);
                    newValue = oldValue + increaseBy;
                }

                _keyValuePairs[member] = newValue;
                _skiplist.Add(member, newValue);

                return newValue;
            }
        }

        public int RemoveRangeByScore(string key, double min, double max)
        {

            var removedMembers = _skiplist.RemoveRangeByScore(min, max);

            foreach (var member in removedMembers)
            {
                _keyValuePairs.Remove(member, out _);
            }

            return removedMembers.Count;
        }

        public int RemoveRangeByRank(string key, int start, int end)
        {

            var removedMembers = _skiplist.RemoveByRank(start, end);

            foreach (var member in removedMembers)
            {
                _keyValuePairs.Remove(member, out _);
            }

            return removedMembers.Count;
        }

        public int? GetRank(string key)
        {
            return _skiplist.GetRank(key);
        }

        public int? GetReversRank(string key)
        {
            return _skiplist.GetReversRank(key);
        }

        public int GetCardinality()
        {
            return _keyValuePairs.Count;
        }

        public int GetAmountByRange(double min, double max)
        {
            return _skiplist.GetByRange(min, max).Count; 
        }

        public List<SkiplistData> GetByRange(double min, double max)
        {
            return _skiplist.GetByRange(min, max);
        }

        public List<SkiplistData> GetByIndex(int start, int end)
        {
            return _skiplist.GetByIndex(start, end);
        }

        public ConcurrentDictionary<string, double> GetAll()
        {
            return _keyValuePairs;
        }

        public bool TryGetScore(string member, out double score) => _keyValuePairs.TryGetValue(member, out score);
    }
}