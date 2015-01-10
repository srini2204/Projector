using System;
using System.Net.Sockets;

namespace Projector.IO.SocketHelpers
{
    public static class SocketExtensions
    {
        public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
        {
            return DoInvoke(socket.ReceiveAsync, awaitable);
        }

        public static SocketAwaitable SendAsync(this Socket socket, SocketAwaitable awaitable)
        {
            return DoInvoke(socket.SendAsync, awaitable);
        }

        public static SocketAwaitable AcceptAsync(this Socket socket, SocketAwaitable awaitable)
        {
            return DoInvoke(socket.AcceptAsync, awaitable);
        }

        public static SocketAwaitable ConnectAsync(this Socket socket, SocketAwaitable awaitable)
        {
            return DoInvoke(socket.ConnectAsync, awaitable);
        }

        public static SocketAwaitable DisconnectAsync(this Socket socket, SocketAwaitable awaitable)
        {
            return DoInvoke(socket.DisconnectAsync, awaitable);
        }

        private static SocketAwaitable DoInvoke(Func<SocketAsyncEventArgs, bool> socketAsyncFunc, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socketAsyncFunc(awaitable.EventArgs))
            {
                awaitable.WasCompleted = true;
            }

            return awaitable;
        }
    }
}
