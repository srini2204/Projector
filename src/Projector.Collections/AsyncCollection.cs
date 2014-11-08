﻿using Projector.Collections.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.Collections
{
    /// <summary>
    /// Represents a thread-safe collection that allows asynchronous consuming.
    /// </summary>
    /// <typeparam name="T">The type of the items contained in the collection.</typeparam>
    public class AsyncCollection<T> : IAsyncCollection<T>
    {
        private IProducerConsumerCollection<T> _itemQueue;
        private ConcurrentQueue<IAwaiter<T>> _awaiterQueue = new ConcurrentQueue<IAwaiter<T>>();

        //	_queueBalance < 0 means there are free awaiters and not enough items.
        //	_queueBalance > 0 means the opposite is true.
        private long _queueBalance = 0;

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncCollection"/> with a specified <see cref="IProducerConsumerCollection{T}"/> as an underlying item storage.
        /// </summary>
        /// <param name="itemQueue">The collection to use as an underlying item storage. MUST NOT be accessed elsewhere.</param>
        public AsyncCollection(IProducerConsumerCollection<T> itemQueue)
        {
            _itemQueue = itemQueue;
            _queueBalance = _itemQueue.Count;
        }

        #region IAsyncCollection<T> members

        /// <summary>
        /// Gets an amount of pending item requests.
        /// </summary>
        public int AwaiterCount
        {
            get { return _awaiterQueue.Count; }
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">The item to add to the collection.</param>
        public void Add(T item)
        {
            while (!TryAdd(item)) ;
        }

        /// <summary>
        /// Tries to add an item to the collection.
        /// May fail if an awaiter that's supposed to receive the item is cancelled. If this is the case, the TryAdd() method must be called again.
        /// </summary>
        /// <param name="item">The item to add to the collection.</param>
        /// <returns>True if the item was added to the collection; false if the awaiter was cancelled and the operation must be retried.</returns>
        private bool TryAdd(T item)
        {
            long balanceAfterCurrentItem = Interlocked.Increment(ref _queueBalance);
            SpinWait spin = new SpinWait();

            if (balanceAfterCurrentItem > 0)
            {
                //	Items are dominating, so we can safely add a new item to the queue.
                while (!_itemQueue.TryAdd(item))
                    spin.SpinOnce();

                return true;
            }
            else
            {
                //	There's at least one awaiter available or being added as we're speaking, so we're giving the item to it.

                IAwaiter<T> awaiter;

                while (!_awaiterQueue.TryDequeue(out awaiter))
                    spin.SpinOnce();

                //	Returns false if the cancellation occurred earlier.
                return awaiter.TrySetResult(item);
            }
        }

        /// <summary>
        /// Removes and returns an item from the collection in an asynchronous manner.
        /// </summary>
        public Task<T> TakeAsync(CancellationToken cancellationToken)
        {
            CompletionSourceAwaiter<T> awaiter = new CompletionSourceAwaiter<T>(cancellationToken);
            return TakeAsync(awaiter);
        }

        private Task<T> TakeAsync(IAwaiter<T> awaiter)
        {
            long balanceAfterCurrentAwaiter = Interlocked.Decrement(ref _queueBalance);

            if (balanceAfterCurrentAwaiter < 0)
            {
                //	Awaiters are dominating, so we can safely add a new awaiter to the queue.

                _awaiterQueue.Enqueue(awaiter);
                return awaiter.Task;
            }
            else
            {
                //	There's at least one item available or being added, so we're returning it directly.

                T item;
                SpinWait spin = new SpinWait();

                while (!_itemQueue.TryTake(out item))
                    spin.SpinOnce();

                return Task.FromResult(item);
            }
        }

        public override string ToString()
        {
            return String.Format("Count = {0}, Awaiters = {1}", Count, AwaiterCount);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _itemQueue.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _itemQueue.GetEnumerator();
        }

        #endregion

        #region ICollection Members

        public int Count
        {
            get { return _itemQueue.Count; }
        }

        public void CopyTo(Array array, int index)
        {
            (_itemQueue as System.Collections.ICollection).CopyTo(array, index);
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { throw new NotSupportedException(); }
        }

        #endregion
    }


}