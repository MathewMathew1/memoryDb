
namespace RedisServer.RadixTree.Service
{
    interface IRadixTree<T>
    {
        void Insert(string key, T value);
        T? TryGet(string key);
        IEnumerable<KeyValuePair<string, T>> Range(string start, string end);
    }
}