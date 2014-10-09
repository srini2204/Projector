using System;
using System.Collections.Concurrent;

namespace Projector.IO.SocketHelpers
{
    public sealed class ObjectPool<T>
    {
        private readonly ConcurrentStack<T> _pool;

        public ObjectPool()
        {
            _pool = new ConcurrentStack<T>();
        }

        internal int Count
        {
            get { return _pool.Count; }
        }

        public T Pop()
        {
            T item;
            _pool.TryPop(out item);

            return item;

        }

        public void Push(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a Pool cannot be null");
            }

            _pool.Push(item);
        }
    }
}
