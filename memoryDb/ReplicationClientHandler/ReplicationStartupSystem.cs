using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisServer.ServerInfo.Model;
using RedisServer.ServerInfo.Service;

namespace RedisServer.Replication.Service
{
    public class ReplicationStartupService : IHostedService
    {
        private readonly IServerInfoService _serverInfo;
        private readonly IReplicationHandshakeClient _handshakeClient;
        private readonly ILogger<ReplicationStartupService> _logger;

        public ReplicationStartupService(
            IServerInfoService serverInfo,
            IReplicationHandshakeClient handshakeClient,
            ILogger<ReplicationStartupService> logger)
        {
            _serverInfo = serverInfo;
            _handshakeClient = handshakeClient;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var masterInfo = _serverInfo.GetMasterInfo();       
            if (_serverInfo is ServerInfoService concrete && masterInfo.Role == Role.SLAVE)
            { 
                string? host = masterInfo.MasterHost;
                int? port = masterInfo.MasterPort;

                if (host != null && port.HasValue)
                {
                    _logger.LogInformation("Starting replication handshake with master at {Host}:{Port}", host, port.Value);
                    await _handshakeClient.PerformHandshakeAsync(host, port.Value, _serverInfo.GetPort());
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
