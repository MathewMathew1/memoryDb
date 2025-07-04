
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using RedisServer.Command.Service;
using RedisServer.CommandHandlers.Model;
using RedisServer.CommandHandlers.Service;
using RedisServer.RdbFile.Service;
using RedisServer.ServerInfo.Service;

namespace RedisServer.Replication.Service
{
    public class ReplicationHandshakeClient : IReplicationHandshakeClient
    {
        private readonly ILogger<ReplicationHandshakeClient> _logger;
        private readonly ICommandParser _commandParser;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ICommandDispatcher _masterCommandHandler;
        private readonly IReplicationMetrics _metrics;
        private readonly IServerInfoService _serverInfoService;
        private readonly IRdbFileService _rdbFileService;

        public ReplicationHandshakeClient(ILogger<ReplicationHandshakeClient> logger, ICommandParser commandParser, CommandDispatcher commandDispatcher,
        IReplicationMetrics metrics, IServerInfoService serverInfoService, IRdbFileService rdbFileService, MasterCommandDispatcher masterCommandDispatcher)
        {
            _logger = logger;
            _commandParser = commandParser;
            _commandDispatcher = commandDispatcher;
            _metrics = metrics;
            _serverInfoService = serverInfoService;
            _rdbFileService = rdbFileService;
            _masterCommandHandler = masterCommandDispatcher;
        }

        public async Task PerformHandshakeAsync(string host, int port, int localPort)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(host, port);
            Socket socket = client.Client;

            await SendAndAwaitResponse("*1\r\n$4\r\nPING\r\n", socket);
            string authPassword = _serverInfoService.GetAuthPassword();
            if (!string.IsNullOrEmpty(authPassword))
            {
                string authCommand = $"*2\r\n$4\r\nAUTH\r\n${authPassword.Length}\r\n{authPassword}\r\n";
                await SendAndAwaitResponse(authCommand, socket);

            }

            await SendAndAwaitResponse(
                $"*3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n${localPort.ToString().Length}\r\n{localPort}\r\n", socket);
            await SendAndAwaitResponse("*3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n", socket);
            byte[] psyncData = Encoding.UTF8.GetBytes("*3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n");
            await socket.SendAsync(psyncData, SocketFlags.None);

            try
            {
                await ReceiveAndStoreRdbAsync(socket);
                _rdbFileService.LoadRdbIntoMemory();
            }
            catch (Exception e)
            {
                _logger.LogError($"{e}");
            }


            await ListenAndProcessIncomingStream(socket);
        }

        private async Task SendAndAwaitResponse(string command, Socket socket)
        {
            byte[] data = Encoding.UTF8.GetBytes(command);
            await socket.SendAsync(data, SocketFlags.None);

            byte[] buffer = new byte[1024];
            int bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);

            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            _logger.LogInformation("Received from master: {Response}", response);
        }

        private async Task ListenAndProcessIncomingStream(Socket socket)
        {
            var buffer = new byte[8192];

            while (true)
            {
                int bytesRead = await socket.ReceiveAsync(buffer);

                if (bytesRead == 0) break;

                var commands = _commandParser.ParseCommands(buffer, bytesRead);

                foreach (var command in commands)
                {

                    if (command.Name.ToLower().StartsWith("replconf"))
                    {
                        var responses = await _masterCommandHandler.DispatchCommand(command, socket);
                        foreach (var response in responses)
                        {
                            await socket.SendAsync(response, SocketFlags.None);
                        }

                    }
                    else
                    {
                        await _commandDispatcher.DispatchCommand(command, socket);
                    }

                    _metrics.BytesReadFromMaster += command.BytesConsumed;
                }


            }

        }

        private async Task ReceiveAndStoreRdbAsync(Socket socket)
        {
            var serverData = _serverInfoService.GetServerDataInfo();
            var fullPath = Path.Combine(serverData.dir, serverData.dbFileName);

            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            using var networkStream = new NetworkStream(socket, ownsSocket: false);

            string fullsyncLine = await ReadLineAsync(networkStream);

            if (string.IsNullOrEmpty(fullsyncLine) || !fullsyncLine.StartsWith("+FULLRESYNC"))
                throw new InvalidDataException("Expected +FULLRESYNC from master");

            string header = await ReadLineAsync(networkStream);

            if (string.IsNullOrEmpty(header) || !header.StartsWith('$'))
                throw new InvalidDataException("Invalid RDB bulk string header from master");

            string strLength = header[1..];

            if (!int.TryParse(strLength, out int length))
                throw new InvalidDataException("Could not parse RDB file length");


            byte[] buffer = new byte[8192];
            int remaining = length;

            while (remaining > 0)
            {
                int toRead = Math.Min(remaining, buffer.Length);
                int read = await networkStream.ReadAsync(buffer.AsMemory(0, toRead));

                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                remaining -= read;
            }

            byte[] trailer = new byte[2];
            int trailerRead = await networkStream.ReadAsync(trailer, 0, 2);

            if (trailerRead != 2 || trailer[0] != '\r' || trailer[1] != '\n')
                throw new InvalidDataException("Missing final CRLF after RDB payload");
        }

        private async Task<string> ReadLineAsync(Stream stream)
        {
            var builder = new StringBuilder();
            var buffer = new byte[1];
            char prev = '\0';

            while (true)
            {
                int read = await stream.ReadAsync(buffer, 0, 1);
                if (read == 0) break;

                char current = (char)buffer[0];
                builder.Append(current);

                if (prev == '\r' && current == '\n')
                    break;

                prev = current;
            }

            return builder.ToString().TrimEnd('\r', '\n');
        }


    }
}
