using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Model;
using RedisServer.Database.Model;
using RedisServer.Database.Service;


namespace RedisServer.CommandHandlers.Service
{
    public class ZADDComand : ICommandHandler
    {
        private readonly ISetService _db;

        private class ZADD_ARGUMENT
        {
            public required string Member { get; set; }
            public required double Value { get; set; }
        }

        public ZADDComand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => CommandType.ZADD.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'zaad'\r\n") };
            }

            List<ZADD_ARGUMENT> values = new List<ZADD_ARGUMENT>();
            var keySet = command.Arguments[0];

            for (int i = 1; i < command.Arguments.Count - 1; i += 2)
            {
                var member = command.Arguments[i + 1];

                if (double.TryParse(command.Arguments[i], out var value))
                {
                    values.Add(new ZADD_ARGUMENT { Member = member, Value = value });
                }
            }

            foreach (var value in values)
            {
                _db.AddOrUpdate(keySet, value.Member, value.Value);
            }

            return new[] { Encoding.UTF8.GetBytes($":{values.Count}\r\n") };
        }
    }

    public class ZSCORECommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZSCORECommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZSCORE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 2)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'zscore'\r\n") };
            }

            var keySet = command.Arguments[0];
            var member = command.Arguments[1];

            var value = _db.TryGetScore(keySet, member);

            if (value == null)
            {
                return new[] { Encoding.UTF8.GetBytes($"$-1\r\n") };

            }

            return new[] { Encoding.UTF8.GetBytes($"${value.ToString().Length}\r\n{value}\r\n") };
        }
    }

    public class ZINCRBYCommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZINCRBYCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => CommandType.ZINCRBY.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'zINCRBY'\r\n") };
            }

            var keySet = command.Arguments[0];
            var member = command.Arguments[2];
            if (!double.TryParse(command.Arguments[1], out var increaseBy)) return new[] { Encoding.UTF8.GetBytes("-ERR improper value type for arg increaseBy  \r\n") };

            var value = _db.IncreaseBy(keySet, member, increaseBy);

            return new[] { Encoding.UTF8.GetBytes($"${value.ToString().Length}\r\n{value}\r\n") };
        }
    }

    public class ZREMCommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZREMCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => CommandType.ZREM.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 2)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'zREM'\r\n") };
            }

            var keySet = command.Arguments[0];
            var members = new List<string>();
            Console.WriteLine("DELETE");
            for (int i = 1; i < command.Arguments.Count; i++)
            {
                members.Add(command.Arguments[i]);
            }

            for (int i = 0; i < members.Count; i++)
            {
                _db.DeleteMember(keySet, members[i]);
            }

            return new[] { Encoding.UTF8.GetBytes($":{members.Count}\r\n") };
        }
    }

    public class ZREMRANGEBYSCORECommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZREMRANGEBYSCORECommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => CommandType.ZREMRANGEBYSCORE.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count != 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'zremrangebyscore'\r\n") };
            }

            var key = command.Arguments[0];
            if (!double.TryParse(command.Arguments[1], out var min) || !double.TryParse(command.Arguments[2], out var max))
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR min or max is not a valid double\r\n") };
            }

            var removed = _db.RemoveRangeByScore(key, min, max);
            return new[] { Encoding.UTF8.GetBytes($":{removed}\r\n") };
        }
    }

    public class ZREMRANGEBYRANKCommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZREMRANGEBYRANKCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => CommandType.ZREMRANGEBYRANK.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count != 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'zremrangebyrank'\r\n") };
            }

            var key = command.Arguments[0];
            if (!int.TryParse(command.Arguments[1], out var start) || !int.TryParse(command.Arguments[2], out var end))
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR start or end is not a valid double\r\n") };
            }

            var removed = _db.RemoveRangeByRank(key, start, end);
            return new[] { Encoding.UTF8.GetBytes($":{removed}\r\n") };
        }
    }

    public class ZRankCommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZRankCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZRANK";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count != 2)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZRank'\r\n") };
            }

            var key = command.Arguments[0];
            var member = command.Arguments[1];

            var rank = _db.GetRank(key, member);
            if (rank == null)
            {
                return new[] { Encoding.UTF8.GetBytes(":-1\r\n") };
            }

            return new[] { Encoding.UTF8.GetBytes($":{rank}\r\n") };
        }
    }

    public class ZReverseRankCommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZReverseRankCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZREVRANK";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count != 2)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZRank'\r\n") };
            }

            var key = command.Arguments[0];
            var member = command.Arguments[1];

            var rank = _db.GetReverseRank(key, member);
            if (rank == null)
            {
                return new[] { Encoding.UTF8.GetBytes(":-1\r\n") };
            }

            return new[] { Encoding.UTF8.GetBytes($":{rank}\r\n") };
        }
    }


    public class ZCardCommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZCardCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZCard";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count != 1)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZCard'\r\n") };
            }

            var key = command.Arguments[0];
            var count = _db.GetCardinality(key);


            return new[] { Encoding.UTF8.GetBytes($":{count}\r\n") };
        }
    }

    public class ZCountCommand : ICommandHandler
    {
        private readonly ISetService _db;

        public ZCountCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZCount";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count != 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZCard'\r\n") };
            }

            var key = command.Arguments[0];

            if (!double.TryParse(command.Arguments[1], out var min) || !double.TryParse(command.Arguments[2], out var max))
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR min or max is not a valid double\r\n") };
            }

            var count = _db.GetAmountByRange(key, min, max);


            return new[] { Encoding.UTF8.GetBytes($":{count}\r\n") };
        }
    }

}
