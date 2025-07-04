using System.Collections.Concurrent;
using System.Net.Sockets;
using RedisServer.Command.Model;
using RedisServer.Connection.Model;

namespace RedisServer.Connection.Service
{
    public class ConnectionManager
    {
        private ConcurrentDictionary<Socket, ConnectionState> _connections = new ConcurrentDictionary<Socket, ConnectionState>();

        public ConnectionState? GetSocketConnection(Socket socket)
        {
            return _connections[socket];
        }

        public void AddSocket(Socket socket, bool isAuth)
        {
            _connections.TryAdd(socket, new ConnectionState { socket = socket, isAuth = isAuth });
        }

        public void ChangeAuthSocket(Socket socket, bool isAuth)
        {
            if (_connections.TryGetValue(socket, out var state))
            {
                state.isAuth = isAuth;
            }
        }

        public void ChangeLockSocket(Socket socket, bool lockStatus)
        {
            if (_connections.TryGetValue(socket, out var state))
            {
                state.isLocked = lockStatus;
            }

        }

        public void AbortTransaction(Socket socket)
        {
            if (_connections.TryGetValue(socket, out var state))
            {
                state.isLocked = false;
                state.CommandsInQueue = new Queue<ParsedCommand>();
            }

        }

        public void AddCommandToQueue(Socket socket, ParsedCommand command)
        {
            if (_connections.TryGetValue(socket, out var state))
            {
                lock (state.CommandsInQueue)
                {
                    state.CommandsInQueue.Enqueue(command);
                }
            }
        }

        public void RemoveSocket(Socket socket)
        {
            _connections.TryRemove(socket, out _);
        }

        public void AddOnDisconnectEvent(Socket socket, Action<Socket> actionEvent)
        {
            if (!_connections.TryGetValue(socket, out var state)) return;

            lock (state.OnDisconnectEvents)
            {
                state.OnDisconnectEvents.Add(actionEvent);
            }
        }

        public void RemoveOnDisconnectEvent(Socket socket, Action<Socket> actionEvent)
        {
            if (!_connections.TryGetValue(socket, out var state)) return;

            lock (state.OnDisconnectEvents)
            {
                state.OnDisconnectEvents.Remove(actionEvent);
            }
        }


        public void Disconnect(Socket socket)
        {
            if (!_connections.TryGetValue(socket, out var state)) return;

            List<Action<Socket>> actions;
            lock (state.OnDisconnectEvents)
            {
                actions = new List<Action<Socket>>(state.OnDisconnectEvents);
            }

            foreach (var action in actions)
            {
                action(socket);
            }

            _connections.TryRemove(socket, out _);
        }
    }

}