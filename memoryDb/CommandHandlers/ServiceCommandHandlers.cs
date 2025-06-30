using Microsoft.Extensions.DependencyInjection;
using RedisServer.Command.Service;

namespace RedisServer.CommandHandlers.Service
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisCommandHandlers(this IServiceCollection services)
        {
            services.AddSingleton<ICommandHandler, PingCommand>();
            services.AddSingleton<ICommandHandler, EchoCommand>();
            services.AddSingleton<ICommandHandler, GetCommand>();
            services.AddSingleton<ICommandHandler, SetCommand>();
            services.AddSingleton<ICommandHandler, InfoCommand>();
            services.AddSingleton<ICommandHandler, PsyncCommand>();
            services.AddSingleton<ICommandHandler, WaitCommand>();
            services.AddSingleton<ICommandHandler, ReplConfCommandHandler>();
            services.AddSingleton<ICommandHandler, GetTypeCommand>();
            services.AddSingleton<ICommandHandler, XAddCommand>();
            services.AddSingleton<ICommandHandler, XRangeCommand>();
            services.AddSingleton<ICommandHandler, XReadCommand>();
            services.AddSingleton<ICommandHandler, IncreaseCommand>();
            services.AddSingleton<ICommandHandler, MultiCommand>();
            services.AddSingleton<ICommandHandler, ConfigCommand>();
            services.AddSingleton<ICommandHandler, KeysCommand>();
            services.AddSingleton<ICommandHandler, LPushCommand>();
            services.AddSingleton<ICommandHandler, RPushCommand>();
            services.AddSingleton<ICommandHandler, PopLeftCommand>();
            services.AddSingleton<ICommandHandler, PopRightCommand>();
            services.AddSingleton<ICommandHandler, RangeLeftCommand>();
            services.AddSingleton<ICommandHandler, GetLenCommand>();
            services.AddSingleton<ICommandHandler, RemoveValuesCommand>();
            services.AddSingleton<ICommandHandler, AuthCommand>();

            services.AddSingleton<ICommandMetaHandler, ExecCommand>();
            services.AddSingleton<ICommandMetaHandler, DiscardCommand>();

            services.AddSingleton<IMasterCommandHandler, AckCommand>();

            services.AddSingleton<IMetaCommandDispatcher, MetaCommandDispatcher>();
            services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

            return services;
        }
    }
}
