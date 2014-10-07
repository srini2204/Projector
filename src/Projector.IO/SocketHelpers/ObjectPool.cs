using System;
using System.Collections.Concurrent;

namespace Projector.IO.SocketHelpers
{
    internal sealed class ObjectPool<T>
    {
        private readonly ConcurrentStack<T> _pool;

        internal ObjectPool()
        {
            _pool = new ConcurrentStack<T>();
        }

        internal int Count
        {
            get { return _pool.Count; }
        }

        internal T Pop()
        {
            T item;
            _pool.TryPop(out item);

            return item;

        }

        internal void Push(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a Pool cannot be null");
            }

            _pool.Push(item);
        }
    }
}
