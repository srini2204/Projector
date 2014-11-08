﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Utils
{
    class ReusableTaskCompletionSource<TResult> : INotifyCompletion
    {

        private readonly static Action SENTINEL = () => { };

        internal bool WasCompleted;
        internal Action Continuation;
        private TResult _result;

        public void SetResult(TResult result)
        {
            _result = result;

            var prev = Continuation ?? Interlocked.CompareExchange(ref Continuation, SENTINEL, null);
            if (prev != null)
            {
                Task.Run(prev);
            }
        }

        public void OnCompleted(Action continuation)
        {
            if (Continuation == SENTINEL || Interlocked.CompareExchange(ref Continuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        internal void Reset()
        {
            WasCompleted = false;
            Continuation = null;
        }

        public ReusableTaskCompletionSource<TResult> GetAwaiter() { return this; }

        public bool IsCompleted { get { return WasCompleted; } }

        public TResult GetResult()
        {
            return _result;
        }

    }
}
