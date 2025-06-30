
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisServer.Database.Model;
using RedisServer.Database.Service;

namespace RedisServer.BackgroundKeyCleaners.Service
{
    public class BackgroundKeyCleaner : IHostedService, IDisposable
    {
        private readonly ILogger<BackgroundKeyCleaner> _logger;
        private Timer? _timer = null;
        private readonly IStringService _memoryDatabase;

        public BackgroundKeyCleaner(ILogger<BackgroundKeyCleaner> logger, IStringService memoryDatabase)
        {
            _logger = logger;
            _memoryDatabase = memoryDatabase;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(0.1));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
             _memoryDatabase.CleanDataSet();
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

    }
}