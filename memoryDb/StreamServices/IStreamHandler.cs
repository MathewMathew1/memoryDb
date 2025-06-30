

using RedisServer.StreamServices.Model;

namespace RedisServer.StreamServices.Service
{
    public interface IStreamIdHandler
    {
        long? LastMillisecondsId { get; }
        int? SequenceNumber { get; }
        ParsedSteamId HandleId(string id);
        void SetNewData(long newMilliSeconds, int sequenceNumber);
        int AutoGenerateSequenceForMilliseconds(long newMilliSeconds);
        public ParsedSteamIdWithNumber AutoGenerateSequence();
    }
}