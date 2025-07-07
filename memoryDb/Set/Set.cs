using System.Collections.Concurrent;
using RedisServer.Database.Model;

namespace RedisServer.Database.Service
{
    public class SetService : ISetService
    {
        private readonly ConcurrentDictionary<string, ZSet> _zSets = new ConcurrentDictionary<string, ZSet>();
        private readonly object _lock = new();



        public void AddOrUpdate(string setKey, string member, double value)
        {
            lock (_lock)
            {
                if (!_zSets.TryGetValue(setKey, out var zSet))
                {
                    _zSets[setKey] = new ZSet();

                }

                _zSets[setKey].AddOrUpdate(member, value);
            }
        }

        public double IncreaseBy(string setKey, string member, double increaseBy)
        {
            lock (_lock)
            {
                if (!_zSets.TryGetValue(setKey, out var zSet))
                {
                    _zSets[setKey] = new ZSet();

                }
                Console.WriteLine($" increaseBy{increaseBy}");
                return _zSets[setKey].IncreaseBy(member, increaseBy);
            }
        }

        public void DeleteMember(string setKey, string member)
        {
            lock (_lock)
            {
                if (!_zSets.TryGetValue(setKey, out var zSet))
                {
                    return;
                }

                _zSets[setKey].Delete(member);
            }
        }


        public double? TryGetScore(string setKey, string member)
        {

            _zSets.TryGetValue(setKey, out var zSet);

            if (zSet == null) return null;

            var valueExists = zSet.TryGetScore(member, out var score);
            if (!valueExists) return null;

            return score;
        }

        public int RemoveRangeByScore(string setKey, double min, double max)
        {
            lock (_lock)
            {
                if (!_zSets.TryGetValue(setKey, out var zSet))
                {
                    return 0;
                }

                return _zSets[setKey].RemoveRangeByScore(setKey, min, max);
            }
        }

        public int RemoveRangeByRank(string setKey, int start, int end)
        {
            lock (_lock)
            {
                if (!_zSets.TryGetValue(setKey, out var zSet))
                {
                    return 0;
                }

                return _zSets[setKey].RemoveRangeByRank(setKey, start, end);
            }
        }

        public int? GetRank(string setKey, string member)
        {
            lock (_lock)
            {
                if (!_zSets.TryGetValue(setKey, out var zSet))
                {
                    return null;
                }

                return zSet.GetRank(member);
            }
        }

        
        public int? GetReverseRank(string setKey, string member)
        {
            lock (_lock)
            {
                if (!_zSets.TryGetValue(setKey, out var zSet))
                {
                    return null;
                }

                return zSet.GetReversRank(member);
            }
        }
    }
}