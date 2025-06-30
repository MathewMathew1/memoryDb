
using System.Collections.Concurrent;

namespace RedisServer.RadixTree.Service
{
    class RadixTreeNode<T>
    {
        public string KeyPart = "";
        public ConcurrentDictionary<char, RadixTreeNode<T>> Children = new ConcurrentDictionary<char, RadixTreeNode<T>>();
        public T? Value;
        public bool HasValue = false;
    }
}