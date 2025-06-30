using System.Net.Sockets;
using System.Text;
using RedisServer.Command.Model;
using RedisServer.RdbFile.Service;
using RedisServer.Replication.Service;
using RedisServer.ServerInfo.Service;


namespace RedisServer.CommandHandlers.Service
{
    public class PsyncCommand : ICommandHandler
    {
        private readonly IServerInfoService _serverInfoService;
        private readonly IReplicaSocketService _replicaSocketService;
        private readonly IRdbFileBuilderService _rdbFileBuilderService;

        public PsyncCommand(IServerInfoService serverInfoService, IReplicaSocketService replicaSocketService, IRdbFileBuilderService rdbFileBuilderService)
        {
            _serverInfoService = serverInfoService;
            _replicaSocketService = replicaSocketService;
            _rdbFileBuilderService = rdbFileBuilderService;
        }

        public string CommandName => "PSYNC";

        public async Task<IEnumerable<byte[]>> Handle(ParsedCommand command, Socket socket)
        {
            var responses = new List<byte[]>();

            var info = _serverInfoService.GetServerDataInfo();

            _replicaSocketService.AddReplica(socket);

            responses.Add(Encoding.UTF8.GetBytes($"+FULLRESYNC {info.MasterReplid} 0\r\n"));

            _rdbFileBuilderService.WriteRdbFromMemory();

            var serverData = _serverInfoService.GetServerDataInfo();
            var fullPath = Path.Combine(serverData.dir, serverData.dbFileName);

            byte[] rdbBinary = File.ReadAllBytes(fullPath);


            responses.Add(Encoding.UTF8.GetBytes($"${rdbBinary.Length}\r\n"));
            responses.Add(rdbBinary);
            responses.Add(Encoding.UTF8.GetBytes("\r\n"));

            return responses;
        }

    }


}