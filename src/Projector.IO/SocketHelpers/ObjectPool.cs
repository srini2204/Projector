using System;
using System.Collections.Generic;
using System.Threading;

namespace SocketAsyncServer
{
    internal sealed class ObjectPool<T>
    {
        private Int32 nextTokenId = 0;

        private Stack<T> _pool;

        internal ObjectPool(Int32 capacity)
        {
            _pool = new Stack<T>(capacity);
        }

        internal Int32 Count
        {
            get { return _pool.Count; }
        }

        internal Int32 AssignTokenId()
        {
            Int32 tokenId = Interlocked.Increment(ref nextTokenId);
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
