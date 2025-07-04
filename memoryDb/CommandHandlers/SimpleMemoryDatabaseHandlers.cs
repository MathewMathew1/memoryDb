using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Model;
using RedisServer.Database.Model;
using RedisServer.Database.Service;


namespace RedisServer.CommandHandlers.Service
{
    public class GetCommand : ICommandHandler
    {
        private readonly IStringService _db;

        public GetCommand(IStringService db)
        {
            _db = db;
        }

        public string CommandName => "get";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count == 0)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'get'\r\n") };
            }


            var value = _db.Get(command.Arguments[0]);

            if (value == null)
                return new[] { Encoding.UTF8.GetBytes("$-1\r\n") };

            return new[] { Encoding.UTF8.GetBytes($"${value.Length}\r\n{value}\r\n") };
        }
    }

    public class GetTypeCommand : ICommandHandler
    {
        private readonly IMemoryDatabaseRouter _db;

        public GetTypeCommand(IMemoryDatabaseRouter db)
        {
            _db = db;
        }

        public string CommandName => "type";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count == 0)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'type'\r\n") };
            }


            var value = _db.GetType(command.Arguments[0]);

            return new[] { Encoding.UTF8.GetBytes($"+{value}\r\n") };
        }
    }

    public class SetCommand : ICommandHandler
    {
        private readonly IStringService _db;

        public SetCommand(IStringService db)
        {
            _db = db;
        }

        public string CommandName => CommandType.Set.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'set'\r\n") };

            double? expirationTime = null;
            var flagHandlers = new Dictionary<string, Action<string>>
            {
                ["px"] = value =>
                {
                    if (double.TryParse(value, out var ms))
                        expirationTime = ms;
                }
            };

            for (int i = 2; i + 1 < command.Arguments.Count; i += 2)
            {
                var flag = command.Arguments[i].ToLowerInvariant();
                var value = command.Arguments[i + 1];

                if (flagHandlers.TryGetValue(flag, out var handler))
                {
                    handler(value);
                }

            }

            _db.Set(command.Arguments[0], command.Arguments[1], new SetKeyParameters
            {
                expirationTime = expirationTime
            });
            return new[] { Encoding.UTF8.GetBytes("+OK\r\n") };
        }
    }

    public class IncreaseCommand : ICommandHandler
    {
        private readonly IStringService _db;

        public IncreaseCommand(IStringService db)
        {
            _db = db;
        }

        public string CommandName => "INCR";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 1)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'Incr'\r\n") };

            var value = _db.Increase(command.Arguments[0]);
            if (value == null)
            {
                return new[] { Encoding.UTF8.GetBytes($"-ERR value is not an integer or out of range\r\n") };
            }

            return new[] { Encoding.UTF8.GetBytes($":{value}\r\n") };
        }
    }

      public class IncrbyCommand : ICommandHandler
    {
        private readonly IStringService _db;

        public IncrbyCommand(IStringService db)
        {
            _db = db;
        }

        public string CommandName => "incrby";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'Incr'\r\n") };
            
            var key = command.Arguments[0];
            var increaseBy = int.Parse(command.Arguments[1]);
       
            var value = _db.IncreaseBy(key, increaseBy);
            if (value == null)
            {
                return new[] { Encoding.UTF8.GetBytes($"-ERR value is not an integer or out of range\r\n") };
            }

            return new[] { Encoding.UTF8.GetBytes($":{value}\r\n") };
        }
    }
}



