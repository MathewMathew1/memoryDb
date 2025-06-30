using System.Net.Sockets;
using RedisServer.Command.Model;

namespace RedisServer.CommandHandlers.Service
{
    public interface ICommandHandler
    {
        string CommandName { get; }
        Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket);
    }

    public interface IMasterCommandHandler
    {
        string CommandName { get; }
        Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket);
    }

    public interface ICommandMetaHandler
    {
        string CommandName { get; }
        Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket);
    }
}