

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RedisServer.Publish.Service
{
    public class PublishService : IPublishService
    {
        private ConcurrentDictionary<string, HashSet<Socket>> _subscriptions = new ConcurrentDictionary<string, HashSet<Socket>>();
        private readonly object _lock = new();

        private readonly ILogger<PublishService> _logger;

        public PublishService(ILogger<PublishService> logger)
        {
            _logger = logger;
        }

        public void AddSubscription(string channel, Socket socket)
        {
            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(channel, out var sockets))
                {
                    _subscriptions[channel] = new HashSet<Socket>();
                }

                _subscriptions[channel].Add(socket);
            }
        }

        public void Unsubscribe(string channel, Socket client)
        {
            lock (_lock)
            {
                if (_subscriptions.TryGetValue(channel, out var clients))
                {
                    clients.Remove(client);
                    if (clients.Count == 0)
                        _subscriptions.Remove(channel, out _);
                }
            }
        }

        public int Publish(string channel, string message)
        {
            HashSet<Socket> targets;
            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(channel, out var clients)) return 0;
                targets = new HashSet<Socket>(clients);
            }

            var msg = $"*3\r\n$7\r\nmessage\r\n${channel.Length}\r\n{channel}\r\n${message.Length}\r\n{message}\r\n";
            var response = Encoding.UTF8.GetBytes(msg);

            foreach (var socket in targets)
            {
                try
                {
                    socket.Send(response);
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Error handling publish to connected sockets {e}");
                }
            }

            return targets.Count;
        }

        public int SubscriptionCount(Socket socket)
        {
            lock (_lock)
            {
                var count = 0;
                foreach (var item in _subscriptions)
                {
                    if (item.Value.Contains(socket))
                    {
                        count += 1;
                    }
                }

                return count;
            }
        }


    }
}