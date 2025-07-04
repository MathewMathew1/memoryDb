

using System.Net.Sockets;
using RedisServer.Command.Model;


namespace RedisServer.Connection.Model
{
    public class ConnectionState
    {
        public required Socket socket;
        public bool isLocked { get; set; } = false;
        public bool isAuth { get; set; } = false;
        public bool IsReplica { get; set; } = false;
        public bool ShouldSendRdb { get; set; } = false;
        public Queue<ParsedCommand> CommandsInQueue { get; set; } = new Queue<ParsedCommand>();
        public List<Action<Socket>> OnDisconnectEvents{ get; set; } = new List<Action<Socket>>();
    }
}