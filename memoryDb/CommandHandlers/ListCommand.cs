using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Model;
using RedisServer.Database.Model;
using RedisServer.Database.Service;


namespace RedisServer.CommandHandlers.Service
{
    public class LPushCommand : ICommandHandler
    {
        private readonly IListDatabase _db;

        public LPushCommand(IListDatabase db)
        {
            _db = db;
        }

        public string CommandName => CommandType.LPUSH.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 2)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'left push'\r\n") };
            }

            var key = command.Arguments[0];
            var value = command.Arguments[1];

            _db.AddLeft(key, value);

            return new[] { Encoding.UTF8.GetBytes($"+OK\r\n") };
        }
    }

    public class RPushCommand : ICommandHandler
    {
        private readonly IListDatabase _db;

        public RPushCommand(IListDatabase db)
        {
            _db = db;
        }

        public string CommandName => CommandType.RPUSH.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 2)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'push right'\r\n") };
            }

            var key = command.Arguments[0];
            var value = command.Arguments[1];

            _db.AddRight(key, value);

            return new[] { Encoding.UTF8.GetBytes($"+OK\r\n") };
        }
    }

    public class PopLeftCommand : ICommandHandler
    {
        private readonly IListDatabase _db;

        public PopLeftCommand(IListDatabase db)
        {
            _db = db;
        }

        public string CommandName => CommandType.LPOP.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 1)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'pop left'\r\n") };
            }

            var key = command.Arguments[0];

            var value = _db.PopLeft(key);

            if (value == null)
                return new[] { Encoding.UTF8.GetBytes("$-1\r\n") };

            var valBytes = Encoding.UTF8.GetBytes(value);
            return new[]
            {
                Encoding.UTF8.GetBytes($"${valBytes.Length}\r\n"),
                valBytes,
                Encoding.UTF8.GetBytes("\r\n")
            };
        }
    }

    public class PopRightCommand : ICommandHandler
    {
        private readonly IListDatabase _db;

        public PopRightCommand(IListDatabase db)
        {
            _db = db;
        }

        public string CommandName => CommandType.RPOP.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 1)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'pop left'\r\n") };
            }

            var key = command.Arguments[0];

            var value = _db.PopRight(key);

            if (value == null)
                return new[] { Encoding.UTF8.GetBytes("$-1\r\n") };

            var valBytes = Encoding.UTF8.GetBytes(value);
            return new[]
            {
                Encoding.UTF8.GetBytes($"${valBytes.Length}\r\n"),
                valBytes,
                Encoding.UTF8.GetBytes("\r\n")
            };
        }
    }



    public class RangeLeftCommand : ICommandHandler
    {
        private readonly IListDatabase _db;

        public RangeLeftCommand(IListDatabase db)
        {
            _db = db;
        }

        public string CommandName => "LRange";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'pop left'\r\n") };
            }

            var key = command.Arguments[0];
            var start = int.Parse(command.Arguments[1]);
            var end = int.Parse(command.Arguments[2]);

            var values = _db.GetRange(key, start, end);

            var result = new List<byte[]>
            {
                Encoding.UTF8.GetBytes($"*{values.Count}\r\n")
            };

            foreach (var val in values)
            {
                var valBytes = Encoding.UTF8.GetBytes(val);
                result.Add(Encoding.UTF8.GetBytes($"${valBytes.Length}\r\n"));
                result.Add(valBytes);
                result.Add(Encoding.UTF8.GetBytes("\r\n"));
            }

            return result;
        }
    }

    public class GetLenCommand : ICommandHandler
    {
        private readonly IListDatabase _db;

        public GetLenCommand(IListDatabase db)
        {
            _db = db;
        }

        public string CommandName => "llen";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 1)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'get len'\r\n") };
            }

            var key = command.Arguments[0];

            var value = _db.GetNumberOfElements(key);

            if (value == null)
                return new[] { Encoding.UTF8.GetBytes(":-1\r\n") };

            return new[] { Encoding.UTF8.GetBytes($":{value}\r\n") };
        }
    }

    public class RemoveValuesCommand : ICommandHandler
    {
        private readonly IListDatabase _db;

        public RemoveValuesCommand(IListDatabase db)
        {
            _db = db;
        }

        public string CommandName => CommandType.LREM.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 2)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'l rem'\r\n") };
            }

            var key = command.Arguments[0];
            if (!int.TryParse(command.Arguments[1], out var count))
                return new[] { Encoding.UTF8.GetBytes("-ERR count is not an integer\r\n") };
            var value = command.Arguments[2];

            var removedElements = _db.RemoveValue(key, count, value);

            return new[] { Encoding.UTF8.GetBytes($":{removedElements}\r\n") };
        }
    }

}

