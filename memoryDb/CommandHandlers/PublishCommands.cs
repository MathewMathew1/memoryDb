using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.Publish.Service;


namespace RedisServer.CommandHandlers.Service
{
    public class SubscribeCommand : ICommandHandler
    {
        private readonly IPublishService _publishService;


        public SubscribeCommand(IPublishService publishService)
        {
            _publishService = publishService;
        }

        public string CommandName => "SUBSCRIBE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 1)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'subscribe'\r\n") };
            var channelName = command.Arguments[0];

            _publishService.AddSubscription(channelName, socket);

            var amountOfSubscriptions = _publishService.SubscriptionCount(socket);

            return new[]
            {
                Encoding.UTF8.GetBytes(
                    $"*3\r\n" +
                    $"${"subscribe".Length}\r\nsubscribe\r\n" +
                    $"${channelName.Length}\r\n{channelName}\r\n" +
                    $"${amountOfSubscriptions.ToString().Length}\r\n{amountOfSubscriptions}\r\n"
                )
            };
        }

    }

    public class UnSubscribeCommand : ICommandHandler
    {
        private readonly IPublishService _publishService;


        public UnSubscribeCommand(IPublishService publishService)
        {
            _publishService = publishService;
        }

        public string CommandName => "UNSUBSCRIBE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 1)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'unsubscribe'\r\n") };
            var channelName = command.Arguments[0];

            _publishService.Unsubscribe(channelName, socket);

            var amountOfSubscriptions = _publishService.SubscriptionCount(socket);

            return new[]
            {
                Encoding.UTF8.GetBytes(
                $"*3\r\n" +
                $"${"unsubscribe".Length}\r\nunsubscribe\r\n" +
                $"${channelName.Length}\r\n{channelName}\r\n" +
                $"${amountOfSubscriptions.ToString().Length}\r\n{amountOfSubscriptions}\r\n"
            )
            };
        }

    }

    public class PublishCommand : ICommandHandler
    {
        private readonly IPublishService _publishService;


        public PublishCommand(IPublishService publishService)
        {
            _publishService = publishService;
        }

        public string CommandName => "Publish";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'publish'\r\n") };
            var channelName = command.Arguments[0];
            var message = command.Arguments[1];

            var receivers = _publishService.Publish(channelName, message);

            return new[] {
                Encoding.UTF8.GetBytes($":{receivers}\r\n")
            };
        }





    }

    public class PUnSubscribeCommand : ICommandHandler
    {
        private readonly IPublishService _publishService;


        public PUnSubscribeCommand(IPublishService publishService)
        {
            _publishService = publishService;
        }

        public string CommandName => "PUNSUBSCRIBE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 1)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'punsubscribe'\r\n") };

            var pattern = command.Arguments[0];

            _publishService.RemovePatternSubscription(pattern, socket);

            var amountOfSubscriptions = _publishService.SubscriptionCount(socket);

            return new[]
            {
                Encoding.UTF8.GetBytes(
                    $"*3\r\n" +
                    $"${"punsubscribe".Length}\r\npunsubscribe\r\n" +
                    $"${pattern.Length}\r\n{pattern}\r\n" +
                    $"${amountOfSubscriptions.ToString().Length}\r\n{amountOfSubscriptions}\r\n"
                )
            };
        }

    }

    public class PSubscribeCommand : ICommandHandler
    {
        private readonly IPublishService _publishService;


        public PSubscribeCommand(IPublishService publishService)
        {
            _publishService = publishService;
        }

        public string CommandName => "PSUBSCRIBE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 1)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'psubscribe'\r\n") };
            var pattern = command.Arguments[0];

            _publishService.AddPatternSubscription(pattern, socket);

            var amountOfSubscriptions = _publishService.SubscriptionCount(socket);

            return new[]
            {
                Encoding.UTF8.GetBytes(
                    $"*3\r\n" +
                    $"${"psubscribe".Length}\r\npsubscribe\r\n" +
                    $"${pattern.Length}\r\n{pattern}\r\n" +
                    $"${amountOfSubscriptions.ToString().Length}\r\n{amountOfSubscriptions}\r\n"
                )
            };
        }
    }

}