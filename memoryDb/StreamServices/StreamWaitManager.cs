using System.Collections.Concurrent;
using RedisServer.Database.Model;

namespace RedisServer.StreamServices.Service
{
    public class StreamWaitManager
    {
        private readonly ConcurrentDictionary<string, List<TaskCompletionSource<StreamEntry>>> _waiters =
            new();

        public Task<StreamEntry?> WaitForDataAsync(string key, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<StreamEntry>(TaskCreationOptions.RunContinuationsAsynchronously);
            var list = _waiters.GetOrAdd(key, _ => new List<TaskCompletionSource<StreamEntry>>());

            lock (list)
            {
                list.Add(tcs);
            }

            if (timeout.TotalMilliseconds > 0)
            {
                var timeoutTask = Task.Delay(timeout).ContinueWith(_ =>
                {
                    lock (list)
                    {
                        list.Remove(tcs);
                    }
                    tcs.TrySetResult(null);
                });
            }

            return tcs.Task;
        }

        public void SignalNewData(string key, StreamEntry entry)
        {
            if (!_waiters.TryGetValue(key, out var list)) return;

            List<TaskCompletionSource<StreamEntry>> toSignal;
            lock (list)
            {
                toSignal = new List<TaskCompletionSource<StreamEntry>>(list);
                list.Clear();
            }

            foreach (var waiter in toSignal)
            {
                waiter.TrySetResult(entry);
            }
        }
    }
}
