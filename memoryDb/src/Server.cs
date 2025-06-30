using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisServer.BackgroundKeyCleaners.Service;
using RedisServer.Command.Service;
using RedisServer.CommandHandlers.Service;
using RedisServer.Connection.Service;
using RedisServer.Database.Service;
using RedisServer.Event.Service;
using RedisServer.Listener;
using RedisServer.RdbFile.Service;
using RedisServer.Replication.Service;
using RedisServer.ServerInfo.Service;
using RedisServer.StreamServices.Service;
using RedisServer.Transaction.Service;

namespace RedisServer.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Logs from your program will appear here!");

            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    });
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices((context, services) =>
                {
                    // Server Info Service with args
                    services.AddSingleton<IServerInfoService>(provider =>
                    {
                        return new ServerInfoService(args);
                    });

                    // TcpListener as singleton
                    services.AddSingleton<TcpListener>(provider =>
                    {
                        var serverInfo = provider.GetRequiredService<IServerInfoService>();
                        var port = (serverInfo as ServerInfoService)?.port ?? 6379;
                        var listener = new TcpListener(IPAddress.Any, port);
                        listener.Start();
                        return listener;
                    });

                    // Core services
                    services.AddSingleton<ConnectionManager>();

                    services.AddSingleton<IReplicationMetrics, ReplicationMetrics>();
                    services.AddSingleton<EventLoop>();
                    services.AddSingleton<ICommandParser, CommandParser>();
                    services.AddSingleton<IStringService, StringService>();
                    services.AddSingleton<IStreamService, StreamService>();
                    services.AddSingleton<IListDatabase, ListDatabase>();
                    services.AddSingleton<IMemoryDatabaseRouter, MemoryDatabaseRouter>();
                    services.AddSingleton<IRdbFileBuilderService, RdbFileBuilderService>();
                    services.AddRedisCommandHandlers();
                    services.AddSingleton<ITransactionExecutor, TransactionExecutor>();
                    services.AddSingleton<IReplicaSocketService, ReplicaSocketService>();


                    services.AddSingleton<IRdbFileService, RdbFileService>();
                    services.AddSingleton<IStreamIdHandler, StreamIdHandler>();


                    services.AddSingleton<RedisServerListener>();
                    services.AddHostedService<RedisStartupService>();
                    services.AddSingleton<IReplicationHandshakeClient, ReplicationHandshakeClient>();

                    // Hosted services
                    services.AddHostedService<ReplicationStartupService>();
                    services.AddHostedService<BackgroundKeyCleaner>();




                })
                .Build()
                .Run();

            Thread.Sleep(Timeout.Infinite);
        }
    }

}
