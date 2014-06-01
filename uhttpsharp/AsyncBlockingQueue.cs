using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace uhttpsharp
{
    public class AsyncBlockingQueue<T> : IAsyncBlockingQueue<T>
    {
        private readonly object _syncRoot = new object();
        private readonly Queue<T> _queue;
        private readonly SemaphoreSlim _releaseDequeueEvent;
        private readonly SemaphoreSlim _releaseEnqueueEvent;
        public AsyncBlockingQueue(int capacity)
        {
            _queue = new Queue<T>(capacity);
            _releaseDequeueEvent = new SemaphoreSlim(0, capacity);
            _releaseEnqueueEvent = new SemaphoreSlim(capacity, capacity);
        }

        public async Task<T> Dequeue()
        {
            T item;

            await _releaseDequeueEvent.WaitAsync();
            lock (_syncRoot)
            {
                item = _queue.Dequeue();
            }
            _releaseEnqueueEvent.Release();

            return item;
        }

        public async Task Enqueue(T item)
        {
            await _releaseEnqueueEvent.WaitAsync();

            lock (_syncRoot)
            {
                _queue.Enqueue(item);
            }

            _releaseDequeueEvent.Release();
        }

        public void Dispose()
        {
            _releaseEnqueueEvent.Dispose();
            _releaseDequeueEvent.Dispose();
        }
    }
}