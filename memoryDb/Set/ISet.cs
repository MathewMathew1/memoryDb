namespace RedisServer.Database.Service
{
    public interface ISetService
    {
        public void AddOrUpdate(string setKey, string member, double value);
        public double? TryGetScore(string setKey, string member);
        double IncreaseBy(string setKey, string member, double increaseBy);
        void DeleteMember(string setKey, string member);
        int RemoveRangeByScore(string setKey, double min, double max);
        int RemoveRangeByRank(string setKey, int start, int end);
        int? GetRank(string setKey, string member);
        int? GetReverseRank(string setKey, string member);
        int GetCardinality(string setKey);
    }
} 