

using System.Collections.Concurrent;

namespace RedisServer.Database.Service
{
    public enum DirectionDeletion
    {
        FROM_LEFT,
        FROM_RIGHT,
        EVERYTHING
    }

    public interface IListDatabase
    {
        void AddLeft(string key, string value);
        void AddRight(string key, string value);
        string? PopLeft(string key);
        string? PopRight(string key);
        List<string> GetRange(string key, int start, int end);
        int GetNumberOfElements(string key);
        int RemoveValue(string key, int count, string value);
        ConcurrentDictionary<string, ConcurrentLinkedList> GetSnapshot();
    }

}