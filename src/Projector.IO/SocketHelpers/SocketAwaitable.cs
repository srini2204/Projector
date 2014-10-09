using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.SocketHelpers
{
    public sealed class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action SENTINEL = () => { };

        internal bool WasCompleted;
        internal Action Continuation;
        public SocketAsyncEventArgs EventArgs;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException("eventArgs");
            EventArgs = eventArgs;
            eventArgs.Completed += delegate
            {
                var prev = Continuation ?? Interlocked.CompareExchange(ref Continuation, SENTINEL, null);
                if (prev != null) prev();
            };
        }

        internal void Reset()
        {
            WasCompleted = false;
            Continuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        public bool IsCompleted { get { return WasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (Continuation == SENTINEL || Interlocked.CompareExchange(ref Continuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {

        }

        public int BytesTransferred { get; set; }
    }
}
