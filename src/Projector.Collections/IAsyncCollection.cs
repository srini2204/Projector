﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.Collections
{
    /// <summary>
    /// Represents a thread-safe collection that allows asynchronous consuming.
    /// </summary>
    /// <typeparam name="T">The type of the items contained in the collection.</typeparam>
    public interface IAsyncCollection<T> : IEnumerable<T>, System.Collections.ICollection
    {
        /// <summary>
        /// Gets an amount of pending item requests.
        /// </summary>
        int AwaiterCount { get; }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">The item to add to the collection.</param>
        void Add(T item);

        /// <summary>
        /// Removes and returns an item from the collection in an asynchronous manner.
        /// </summary>
        Task<T> TakeAsync(CancellationToken cancellationToken);
    }

    public static class AsyncCollectionExtensions
    {
        /// <summary>
        /// Removes and returns an item from the collection in an asynchronous manner.
        /// </summary>
        public static Task<T> TakeAsync<T>(this IAsyncCollection<T> collection)
        {
            return collection.TakeAsync(CancellationToken.None);
        }
    }
}