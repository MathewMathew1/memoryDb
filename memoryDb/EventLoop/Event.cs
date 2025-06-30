
using System.Net.Sockets;

namespace RedisServer.Event.Service
{
    public class ConnectionEvent
    {
        public required Socket Socket { get; set; }
        public required string Type { get; set; } 
    }
}
