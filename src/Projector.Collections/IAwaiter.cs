﻿using System.Threading.Tasks;

namespace Projector.Collections.Internal
{
    /// <summary>
    /// Represents an abstract item awaiter.
    /// </summary>
    interface IAwaiter<T>
    {
        /// <summary>
        /// <para>Attempts to complete the awaiter with a specified result.</para>
        /// <para>Returns false if the awaiter has been canceled.</para>
        /// </summary>
        bool TrySetResult(T result);

        /// <summary>
        /// The task that's completed when the awaiter gets the result.
        /// </summary>
        Task<T> Task { get; }
    }
}