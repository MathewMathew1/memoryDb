using System.Collections;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace RedisServer.Event.Service
{
    public class EventLoop
    {
        private readonly Queue<ConnectionEvent> _events = new();
        private readonly Dictionary<Socket, Func<Socket, Task>> _handlers = new();
        private readonly SemaphoreSlim _eventAvailable = new(0);
        private readonly ILogger<EventLoop> _logger;

        public EventLoop(ILogger<EventLoop> logger)
        {
            _logger = logger;
        }

        public void RegisterHandler(Socket socket, Func<Socket, Task> handler)
        {
            _handlers[socket] = handler;

        }

        public void Enqueue(ConnectionEvent ev)
        {
            lock (_events)
            {
                _events.Enqueue(ev);
            }
            _eventAvailable.Release();
        }

        public async Task ProcessEventsAsync()
        {
            while (true)
            {
                await _eventAvailable.WaitAsync();

                ConnectionEvent ev;
                lock (_events)
                {
                    ev =  _events.Dequeue();
                }

                if (_handlers.TryGetValue(ev.Socket, out var handler))
                {
                    _logger.LogInformation($"[{DateTime.UtcNow:HH:mm:ss.fff}] Handler");
                    await handler(ev.Socket);
                }
            }
        }
    }
}