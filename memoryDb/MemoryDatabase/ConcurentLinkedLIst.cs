namespace RedisServer.Database.Service
{
    public class ConcurrentLinkedList
    {
        private readonly LinkedList<string> _list = new();
        private readonly object _lock = new();

        public void PushLeft(string value)
        {
            lock (_lock)
            {
                _list.AddFirst(value);
            }
        }

        public void PushRight(string value)
        {
            lock (_lock)
            {
                _list.AddLast(value);
            }
        }

        public string? PopLeft()
        {
            lock (_lock)
            {
                if (_list.Count == 0) return null;
                var val = _list.First!.Value;
                _list.RemoveFirst();
                return val;
            }
        }

        public string? PopRight()
        {
            lock (_lock)
            {
                if (_list.Count == 0) return null;
                var val = _list.Last!.Value;
                _list.RemoveLast();
                return val;
            }
        }

        public int GetNumberOfElements()
        {
            lock (_lock)
            {
                return _list.Count;
            }
        }

        public List<string> RangeLeft(int start, int stop)
        {
            lock (_lock)
            {
                var result = new List<string>();
                int count = _list.Count;
                if (count == 0) return result;

                if (start < 0) start = count + start;
                if (stop < 0) stop = count + stop;

                start = Math.Max(0, start);
                stop = Math.Min(count - 1, stop);

                if (start > stop) return result;

                var node = _list.First;
                for (int i = 0; i <= stop && node != null; i++, node = node.Next)
                {
                    if (i >= start)
                        result.Add(node.Value);
                }

                return result;
            }
        }

        public LinkedList<string> GetSnapshot()
        {
            return new LinkedList<string>(_list);
        }

        public int RemoveValue(string value, int count)
        {
            lock (_lock)
            {
                int removedCount = 0;

                if (count == 0)
                {
                    var node = _list.First;
                    while (node != null)
                    {
                        var next = node.Next;
                        if (node.Value == value)
                        {
                            _list.Remove(node);
                            removedCount++;
                        }
                        node = next;
                    }
                }
                else if (count > 0)
                {
                    var node = _list.First;
                    while (node != null && removedCount < count)
                    {
                        var next = node.Next;
                        if (node.Value == value)
                        {
                            _list.Remove(node);
                            removedCount++;
                        }
                        node = next;
                    }
                }
                else
                {
                    var node = _list.Last;
                    while (node != null && removedCount < -count)
                    {
                        var prev = node.Previous;
                        if (node.Value == value)
                        {
                            _list.Remove(node);
                            removedCount++;
                        }
                        node = prev;
                    }
                }

                return removedCount;
            }
        }



    }
}