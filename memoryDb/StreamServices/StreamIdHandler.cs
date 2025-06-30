

using System.Collections.Concurrent;
using RedisServer.StreamServices.Model;

namespace RedisServer.StreamServices.Service
{
    public class StreamIdHandler : IStreamIdHandler
    {
        public long? LastMillisecondsId { get; private set; } = null;
        public int? SequenceNumber { get; private set; } = null;
        private ConcurrentDictionary<long, int> _lastSequences = new ConcurrentDictionary<long, int>();

        public ParsedSteamId HandleId(string id)
        {
            var parts = id.Split('-');

            return new ParsedSteamId { Milliseconds = Int32.Parse(parts[0]), Sequence = parts[1] };
        }

        public void SetNewData(long newMilliSeconds, int sequenceNumber)
        {
            LastMillisecondsId = newMilliSeconds;
            SequenceNumber = sequenceNumber;

            _lastSequences.GetOrAdd(newMilliSeconds, _ => sequenceNumber);
        }

        public int AutoGenerateSequenceForMilliseconds(long newMilliSeconds)
        {
            var valueExists = _lastSequences.TryGetValue(newMilliSeconds, out var val);
            int newValue;

            if (!valueExists)
            {
                newValue = newMilliSeconds == 0 ? 1 : 0;
                _lastSequences.GetOrAdd(newMilliSeconds, _ => newValue);
                return newValue;
            }
            newValue = val + 1;
            _lastSequences.GetOrAdd(newMilliSeconds, _ => newValue);
            return newValue;
        }

        public ParsedSteamIdWithNumber AutoGenerateSequence()
        {
            var millSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return new ParsedSteamIdWithNumber { Milliseconds = millSeconds, Sequence = AutoGenerateSequenceForMilliseconds(millSeconds) };
        }

    }
}