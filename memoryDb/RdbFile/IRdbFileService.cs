namespace RedisServer.RdbFile.Service
{
    public interface IRdbFileService
    {
        List<string> GetKeys(string searchKey);
        public void LoadRdbIntoMemory();
    }
}