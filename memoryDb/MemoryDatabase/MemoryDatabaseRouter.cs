namespace RedisServer.Database.Service
{
    public class MemoryDatabaseRouter: IMemoryDatabaseRouter
    {
        private readonly IStringService _stringService;
        private readonly IStreamService _streamService ;

        public MemoryDatabaseRouter(IStringService stringService, IStreamService streamService)
        {
            _streamService = streamService;
            _stringService = stringService;
        }

        public string GetType(string key)
        {
            if (_stringService.Contains(key)) return "string";
            if (_streamService.Contains(key)) return "stream";
            return "none";
        }

    }
}
