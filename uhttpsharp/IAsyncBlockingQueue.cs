using System;
using System.Threading.Tasks;

namespace uhttpsharp
{
    public interface IAsyncBlockingQueue<T> : IDisposable
    {

        /// <summary>
        /// Dequeues an item from the queue
        /// </summary>
        /// <returns></returns>
        Task<T> Dequeue();

        /// <summary>
        /// Enqueues an item to the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        Task Enqueue(T item);

    }
}
