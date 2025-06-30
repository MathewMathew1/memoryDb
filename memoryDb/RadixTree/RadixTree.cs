using System.Collections.Concurrent;

namespace RedisServer.RadixTree.Service
{
    public class RadixTree<T> : IRadixTree<T>
    {
        private readonly RadixTreeNode<T> _root = new RadixTreeNode<T>();

        public void Insert(string key, T value)
        {
            InsertRecursive(_root, key, value);
        }

        private void InsertRecursive(RadixTreeNode<T> node, string key, T value)
        {
            string keyPart = node.KeyPart;
            int minLength = Math.Min(key.Length, keyPart.Length);
            int commonLength = 0;

            while (commonLength < minLength && key[commonLength] == keyPart[commonLength])
                commonLength++;

            string commonPrefix = key.Substring(0, commonLength);
            string nodeSuffix = keyPart.Substring(commonLength);
            string keySuffix = key.Substring(commonLength);


            if (nodeSuffix.Length > 0)
            {
                var child = new RadixTreeNode<T>
                {
                    KeyPart = nodeSuffix,
                    Value = node.Value,
                    HasValue = node.HasValue,
                    Children = node.Children
                };

                node.KeyPart = commonPrefix;
                node.Value = default;
                node.HasValue = false;
                node.Children = new ConcurrentDictionary<char, RadixTreeNode<T>>
                {
                    [nodeSuffix[0]] = child
                };
            }

            if (keySuffix.Length == 0)
            {
                node.Value = value;
                node.HasValue = true;
                node.Children.Clear();
                return;
            }

            char nextChar = keySuffix[0];
            if (!node.Children.TryGetValue(nextChar, out var next))
            {
                node.Children[nextChar] = new RadixTreeNode<T>
                {
                    KeyPart = keySuffix,
                    Value = value,
                    HasValue = true
                };
            }
            else
            {
                InsertRecursive(next, keySuffix, value);
            }
        }

        public T? TryGet(string key)
        {
            return TryGetRecursive(key, _root);
        }

        private T? TryGetRecursive(string key, RadixTreeNode<T> node)
        {
            string nodeKey = node.KeyPart;

            if (key.Length < nodeKey.Length || !key.StartsWith(nodeKey))
                return default;

            string remaining = key.Substring(nodeKey.Length);

            if (remaining == "")
                return node.HasValue ? node.Value : default;

            char nextChar = remaining[0];
            if (!node.Children.TryGetValue(nextChar, out var child))
                return default;

            return TryGetRecursive(remaining, child);
        }

        public IEnumerable<KeyValuePair<string, T>> Range(string start, string? end)
        {
            return RangeRecursive(_root, "", start, end);
        }

        private IEnumerable<KeyValuePair<string, T>> RangeRecursive(
       RadixTreeNode<T> node,
       string prefix,
       string start,
       string? end)
        {
            string fullKey = prefix;

            bool isInRange = fullKey.CompareTo(start) >= 0 &&
                             (end == null || fullKey.CompareTo(end) <= 0);

            if (node.HasValue && isInRange)
                yield return new KeyValuePair<string, T>(fullKey, node.Value!);

            foreach (var child in node.Children.OrderBy(kvp => kvp.Key))
            {
                string nextPrefix = fullKey + child.Value.KeyPart;

                if (nextPrefix.CompareTo(start) < 0)
                    continue;

                if (end != null && nextPrefix.CompareTo(end) > 0)
                    break;

                foreach (var kvp in RangeRecursive(child.Value, nextPrefix, start, end))
                    yield return kvp;
            }
        }


    }
}