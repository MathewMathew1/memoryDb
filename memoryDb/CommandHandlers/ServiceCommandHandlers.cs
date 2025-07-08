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
            services.AddSingleton<ICommandHandler, SubscribeCommand>();
            services.AddSingleton<ICommandHandler, UnSubscribeCommand>();
            services.AddSingleton<ICommandHandler, PublishCommand>();
            services.AddSingleton<ICommandHandler, PUnSubscribeCommand>();
            services.AddSingleton<ICommandHandler, PSubscribeCommand>();
            services.AddSingleton<ICommandHandler, IncrbyCommand>();
            services.AddSingleton<ICommandHandler, ZADDComand>();
            services.AddSingleton<ICommandHandler, ZSCORECommand>();
            services.AddSingleton<ICommandHandler, ZINCRBYCommand>();
            services.AddSingleton<ICommandHandler, ZREMCommand>();
            services.AddSingleton<ICommandHandler, ZREMRANGEBYSCORECommand>();
            services.AddSingleton<ICommandHandler, ZREMRANGEBYRANKCommand>();
            services.AddSingleton<ICommandHandler, ZRankCommand>();
            services.AddSingleton<ICommandHandler, ZReverseRankCommand>();
            services.AddSingleton<ICommandHandler, ZCardCommand>();
            services.AddSingleton<ICommandHandler, ZCountCommand>();
            services.AddSingleton<ICommandHandler, ZRangeByScoreCommand>();
            services.AddSingleton<ICommandHandler, ZRangeCommand>();
            services.AddSingleton<ICommandHandler, ZReverseRangeCommand>();
            services.AddSingleton<ICommandHandler, ZReverseRangeByScoreCommand>();

            services.AddSingleton<ILuaCommandHandler, EvalCommand>();
            services.AddSingleton<ILuaCommandHandler, EvalShaCommand>();
            services.AddSingleton<ILuaCommandHandler, ScriptCommand>();
            services.AddSingleton<ILuaCommandHandler, FlushAllCommand>();

            services.AddSingleton<ICommandMetaHandler, ExecCommand>();
            services.AddSingleton<ICommandMetaHandler, DiscardCommand>();

            services.AddSingleton<IMasterCommandHandler, AckCommand>();

            services.AddSingleton<LuaCommandDispatcher>();
            services.AddSingleton<MasterCommandDispatcher>();
            services.AddSingleton<IMetaCommandDispatcher, MetaCommandDispatcher>();
            services.AddSingleton<CommandDispatcher>();
            

            return services;
        }
    }
}
