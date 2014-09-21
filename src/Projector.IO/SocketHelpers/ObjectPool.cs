using System;
using System.Collections.Generic;
using System.Threading;

namespace Projector.IO.SocketHelpers
{
    internal sealed class ObjectPool<T>
    {
        private int nextTokenId = 0;

        private Stack<T> _pool;

        internal ObjectPool(int capacity)
        {
            _pool = new Stack<T>(capacity);
        }

        internal int Count
        {
            get { return _pool.Count; }
        }

        internal int AssignTokenId()
        {
            int tokenId = Interlocked.Increment(ref nextTokenId);
            return tokenId;
        }

        internal T Pop()
        {
            lock (_pool)
            {
                return _pool.Pop();
            }
        }

        internal void Push(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (_pool)
            {
                _pool.Push(item);
            }
        }
    }
}
