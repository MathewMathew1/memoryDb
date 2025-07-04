using System.Collections.Concurrent;
using RedisServer.Database.Model;

namespace RedisServer.Database.Service
{
    public class StringService : IStringService
    {
        private readonly ConcurrentDictionary<string, ValueInMemory> _strings = new();
        public object SyncRoot { get; } = new();

        public Dictionary<string, ValueInMemory> GetAllSnapshot()
        {
            return new Dictionary<string, ValueInMemory>(_strings);
        }

        public void Set(string key, string value, SetKeyParameters parameters)
        {
            var val = new ValueInMemory
            {
                value = value,
                expirationDate = parameters.expirationTime.HasValue
                    ? DateTime.UtcNow.AddMilliseconds(parameters.expirationTime.Value)
                    : null
            };
            _strings[key] = val;
        }

        public string? Get(string key)
        {
            if (!_strings.TryGetValue(key, out var val)) return null;
            if (val.expirationDate is not null && val.expirationDate <= DateTime.UtcNow)
            {
                _strings.TryRemove(key, out _);
                return null;
            }
            return val.value;
        }

        public bool Contains(string key)
        {
            return Get(key) != null;
        }

        public void Delete(string key)
        {
            _strings.TryRemove(key, out _);
        }

        public int? Increase(string key)
        {
            if (!_strings.TryGetValue(key, out var oldVal))
            {
                Set(key, "1", new SetKeyParameters { });
                return 1;
            }

            var newVal = oldVal;
            if (!int.TryParse(oldVal.value, out int current))
                return null;

            newVal.value = (current + 1).ToString();

            _strings.TryUpdate(key, newVal, oldVal);
            return int.Parse(newVal.value);
        }

        public void CleanDataSet()
        {
            foreach (KeyValuePair<string, ValueInMemory> entry in _strings)
            {
                var expiration = entry.Value.expirationDate;
                if (expiration != null && expiration <= DateTime.UtcNow)
                {
                    Delete(entry.Key);
                }
            }
        }

        public int? IncreaseBy(string key, int increaseBy)
        {
            if (!_strings.TryGetValue(key, out var val))
            {
                return 0;
            }

            var newVal = val;
            if (!int.TryParse(val.value, out int current))
                return null;

            var newValue = current * increaseBy;

            newVal.value = newValue.ToString();

            _strings.TryUpdate(key, newVal, val);

            return newValue;
        }
    }

}