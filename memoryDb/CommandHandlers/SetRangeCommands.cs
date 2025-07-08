using System.Globalization;
using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.Database.Service;


namespace RedisServer.CommandHandlers.Service
{
    public class ZRangeByScoreCommand : ICommandHandler
    {
        private readonly ISetService _db;

        private class ZADD_ARGUMENT
        {
            public required string Member { get; set; }
            public required double Value { get; set; }
        }

        public ZRangeByScoreCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZRANGEBYSCORE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZRANGEBYSCORE'\r\n") };
            }

            var keySet = command.Arguments[0];

            if (!double.TryParse(command.Arguments[1], out var min) || !double.TryParse(command.Arguments[2], out var max))
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR min or max is not a valid double\r\n") };
            }

            var withScores = command.Arguments.Count > 3 &&
                     command.Arguments[3].ToUpperInvariant() == "WITHSCORES";

            var values = _db.GetByRange(keySet, min, max);

            var response = new List<byte[]>();

            int count = withScores ? values.Count * 2 : values.Count;
            response.Add(Encoding.UTF8.GetBytes($"*{count}\r\n"));

            foreach (var value in values)
            {
                response.Add(Encoding.UTF8.GetBytes($"${value.Member.Length}\r\n{value.Member}\r\n"));

                if (withScores)
                {
                    var scoreStr = value.Score.ToString("G", CultureInfo.InvariantCulture);
                    response.Add(Encoding.UTF8.GetBytes($"${scoreStr.Length}\r\n{scoreStr}\r\n"));
                }
            }

            return response;
        }
    }

    public class ZReverseRangeByScoreCommand : ICommandHandler
    {
        private readonly ISetService _db;

        private class ZADD_ARGUMENT
        {
            public required string Member { get; set; }
            public required double Value { get; set; }
        }

        public ZReverseRangeByScoreCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZREVRANGEBYSCORE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZREVRANGEBYSCORE'\r\n") };
            }

            var keySet = command.Arguments[0];

            if (!double.TryParse(command.Arguments[2], out var min) || !double.TryParse(command.Arguments[1], out var max))
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR min or max is not a valid double\r\n") };
            }

            var withScores = command.Arguments.Count > 3 &&
                     command.Arguments[3].ToUpperInvariant() == "WITHSCORES";

            var values = _db.GetByRange(keySet, min, max);

            var response = new List<byte[]>();

            int count = withScores ? values.Count * 2 : values.Count;
            response.Add(Encoding.UTF8.GetBytes($"*{count}\r\n"));

            for (var i= values.Count-1; i >=0; i-- )
            {
                var value = values[i];
                response.Add(Encoding.UTF8.GetBytes($"${value.Member.Length}\r\n{value.Member}\r\n"));

                if (withScores)
                {
                    var scoreStr = value.Score.ToString("G", CultureInfo.InvariantCulture);
                    response.Add(Encoding.UTF8.GetBytes($"${scoreStr.Length}\r\n{scoreStr}\r\n"));
                }
            }

            return response;
        }
    }

    public class ZRangeCommand : ICommandHandler
    {
        private readonly ISetService _db;

        private class ZADD_ARGUMENT
        {
            public required string Member { get; set; }
            public required double Value { get; set; }
        }

        public ZRangeCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZRANGE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZRANGE'\r\n") };
            }

            var keySet = command.Arguments[0];

            if (!int.TryParse(command.Arguments[1], out var start) || !int.TryParse(command.Arguments[2], out var end))
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR start or end is not a valid int\r\n") };
            }

            var withScores = command.Arguments.Count > 3 &&
                     command.Arguments[3].ToUpperInvariant() == "WITHSCORES";

            var values = _db.GetByIndexRange(keySet, start, end);

            var response = new List<byte[]>();

            int count = withScores ? values.Count * 2 : values.Count;
            response.Add(Encoding.UTF8.GetBytes($"*{count}\r\n"));

            foreach (var value in values)
            {
                response.Add(Encoding.UTF8.GetBytes($"${value.Member.Length}\r\n{value.Member}\r\n"));

                if (withScores)
                {
                    var scoreStr = value.Score.ToString("G", CultureInfo.InvariantCulture);
                    response.Add(Encoding.UTF8.GetBytes($"${scoreStr.Length}\r\n{scoreStr}\r\n"));
                }
            }

            return response;
        }
    }

    public class ZReverseRangeCommand : ICommandHandler
    {
        private readonly ISetService _db;

        private class ZADD_ARGUMENT
        {
            public required string Member { get; set; }
            public required double Value { get; set; }
        }

        public ZReverseRangeCommand(ISetService db)
        {
            _db = db;
        }

        public string CommandName => "ZREVRANGE";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {

            if (command.Arguments.Count < 3)
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'ZREVRANGE'\r\n") };
            }

            var keySet = command.Arguments[0];

            if (!int.TryParse(command.Arguments[2], out var start) || !int.TryParse(command.Arguments[1], out var end))
            {
                return new[] { Encoding.UTF8.GetBytes("-ERR start or end is not a valid int\r\n") };
            }

            var withScores = command.Arguments.Count > 3 &&
                     command.Arguments[3].ToUpperInvariant() == "WITHSCORES";

            var values = _db.GetByIndexRange(keySet, start, end);

            var response = new List<byte[]>();

            int count = withScores ? values.Count * 2 : values.Count;
            response.Add(Encoding.UTF8.GetBytes($"*{count}\r\n"));

            for (var i= values.Count-1; i >=0; i-- )
            {
                var value = values[i];
                response.Add(Encoding.UTF8.GetBytes($"${value.Member.Length}\r\n{value.Member}\r\n"));

                if (withScores)
                {
                    var scoreStr = value.Score.ToString("G", CultureInfo.InvariantCulture);
                    response.Add(Encoding.UTF8.GetBytes($"${scoreStr.Length}\r\n{scoreStr}\r\n"));
                }
            }

            return response;
        }
    }
}