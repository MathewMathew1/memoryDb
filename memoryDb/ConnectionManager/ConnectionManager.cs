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
            if (_connections.TryGetValue(socket, out var oldState))
            {
                var newState = new ConnectionState
                {
                    socket = oldState.socket,
                    isAuth = isAuth
                };

                _connections.TryUpdate(socket, newState, oldState);
            }
        }

        public void ChangeLockSocket(Socket socket, bool lockStatus)
        {
            if (_connections.TryGetValue(socket, out var oldState))
            {
                var newState = new ConnectionState
                {
                    socket = oldState.socket,
                    isLocked = lockStatus
                };

                _connections.TryUpdate(socket, newState, oldState);
            }
        }

        public void AbortTransaction(Socket socket)
        {
            if (_connections.TryGetValue(socket, out var oldState))
            {
                var newState = new ConnectionState
                {
                    socket = oldState.socket,
                    isLocked = false,
                    CommandsInQueue = new Queue<ParsedCommand>()
                };

                _connections.TryUpdate(socket, newState, oldState);
            }
        }

        public void AddCommandToQueue(Socket socket, ParsedCommand command)
        {
            if (_connections.TryGetValue(socket, out var state))
            {
                state.CommandsInQueue.Enqueue(command);
            }
        }

        public void RemoveSocket(Socket socket)
        {
            _connections.TryRemove(socket, out _);
        }
    }

}