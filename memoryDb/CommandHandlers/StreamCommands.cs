using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.CommandHandlers.Model;
using RedisServer.Database.Model;
using RedisServer.Database.Service;
using RedisServer.StreamServices.Service;
using RedisServer.Util.Serialization;

namespace RedisServer.CommandHandlers.Service
{
    public class XAddCommand : ICommandHandler
    {
        private readonly IStreamService _streamService;
        private readonly IStreamIdHandler _streamIdHandler;

        public XAddCommand(IStreamService streamService, IStreamIdHandler streamIdHandler)
        {
            _streamService = streamService;
            _streamIdHandler = streamIdHandler;
        }

        public string CommandName => CommandType.XADD.ToString().ToLowerInvariant();

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 2)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'set'\r\n") };


            string idKey = command.Arguments[0];
            string id = command.Arguments[1];

            int sequenceNumber;
            long milliseconds;

            int length;
            if (id != "*")
            {
                length = id.Length;
                var parsedId = _streamIdHandler.HandleId(id);
                milliseconds = parsedId.Milliseconds;
                if (parsedId.Sequence == "*")
                {
                    sequenceNumber = _streamIdHandler.AutoGenerateSequenceForMilliseconds(parsedId.Milliseconds);
                }
                else
                {
                    sequenceNumber = Int32.Parse(parsedId.Sequence);
                }

            }
            else
            {
                var data = _streamIdHandler.AutoGenerateSequence();
                sequenceNumber = data.Sequence;
                milliseconds = data.Milliseconds;
                id = $"{milliseconds}-{sequenceNumber}";
                length = $"{milliseconds}-{sequenceNumber}".Length;
            }

      
            var wrongTime = milliseconds < _streamIdHandler.LastMillisecondsId ||
             (sequenceNumber <= _streamIdHandler.SequenceNumber && milliseconds == _streamIdHandler.LastMillisecondsId);
            var toSmallId = (sequenceNumber < 1 && milliseconds == 0) || sequenceNumber < 0 || milliseconds < 0;

            if (toSmallId)
            {
                return new[] { Encoding.UTF8.GetBytes($"-ERR The ID specified in XADD must be greater than 0-0\r\n") };
            }

            if (wrongTime)
            {
                return new[] { Encoding.UTF8.GetBytes($"-ERR The ID specified in XADD is equal or smaller than the target stream top item\r\n") };
            }

            _streamIdHandler.SetNewData(milliseconds, sequenceNumber);


            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            for (int i = 2; i + 1 < command.Arguments.Count; i += 2)
            {
                var key = command.Arguments[i].ToLowerInvariant();
                var value = command.Arguments[i + 1];

                dictionary[key] = value;
            }

  
            _streamService.AddEntry(idKey, new StreamEntry { Fields = dictionary, Id = id });

            return new[] { Encoding.UTF8.GetBytes($"${length}\r\n{milliseconds}-{sequenceNumber}\r\n") };
        }

    }


    public class XRangeCommand : ICommandHandler
    {
        private readonly IStreamService _streamService;

        public XRangeCommand(IStreamService streamService)
        {
            _streamService = streamService;
        }

        public string CommandName => "XRange";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 1)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'XRange'\r\n") };


            string idKey = command.Arguments[0];

            string start = "0-0";
            string? end = null;

            if (command.Arguments[1] != null)
            {
             
                start = command.Arguments[1] != "-" ? command.Arguments[1] : "0-0";

                end = command.Arguments[2] != "+" ? command.Arguments[2] : null;
            }

            var entries = _streamService.GetEntriesInRange(idKey, start, end);
            var builder = new StringBuilder();

            RespBuilder.AppendStreamEntries(builder, entries);
        
            return new[] { Encoding.UTF8.GetBytes(builder.ToString()) };
        }

    }


    public class XReadCommand : ICommandHandler
    {
        private readonly IStreamService _streamService;

        public XReadCommand(IStreamService streamService)
        {
            _streamService = streamService;
        }

        public string CommandName => "XRead";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            if (command.Arguments.Count < 3)
                return new[] { Encoding.UTF8.GetBytes("-ERR wrong number of arguments for 'XRead'\r\n") };

            List<string> idKeys = new List<string>();
            List<string> starts = new List<string>();
          
            double? blockingTime = null;
            var flagHandlers = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["block"] = value =>
                {
                    if (double.TryParse(value, out var ms))
                        blockingTime = ms;
                }
            };

            for (var i = 0; i < command.Arguments.Count; i++)
            {
                if (command.Arguments[i] == "streams")
                {
                    var amountLeft = command.Arguments.Count - i - 1;
                    for (var b = i + 1; b < command.Arguments.Count - amountLeft / 2; b++)
                    {
                        idKeys.Add(command.Arguments[b]);
                        starts.Add(command.Arguments[b + amountLeft / 2] != "-" ? command.Arguments[b + amountLeft / 2] : "0-0");
                    }

                    break;
                }
                if (flagHandlers.TryGetValue(command.Arguments[i], out var handler))
                {
                    handler(command.Arguments[i + 1]);
                    i++;
                }

            }


            var readTasks = new List<Task<List<StreamEntry>?>>();
            for (int i = 0; i < idKeys.Count; i++)
            {
                var idKey = idKeys[i];
                var start = starts[i];
                var timeout = blockingTime.HasValue ? TimeSpan.FromMilliseconds(blockingTime.Value) : (TimeSpan?)null;
              
                readTasks.Add(_streamService.GetEntriesInReadAsync(idKey, start, timeout));
            }

            await Task.WhenAll(readTasks);
          
            var builder = new StringBuilder();
            RespBuilder.StreamEntriesRead(builder, readTasks, idKeys);

            return new[] { Encoding.UTF8.GetBytes(builder.ToString()) };
        }

    }


}