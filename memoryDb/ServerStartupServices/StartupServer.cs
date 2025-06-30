using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisServer.Listener;

namespace RedisServer.App
{
    public class RedisStartupService : IHostedService
    {
        private readonly RedisServerListener _listener;
        private readonly ILogger<RedisStartupService> _logger;

        public RedisStartupService(RedisServerListener listener, ILogger<RedisStartupService> logger)
        {
            _listener = listener;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = _listener.StartAcceptLoopAsync();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
