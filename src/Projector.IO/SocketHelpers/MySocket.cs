using System;
using System.Net.Sockets;

namespace Projector.IO.SocketHelpers
{
    public class MySocket
    {
        private readonly Socket _socket;

        public MySocket()
        {

        }

        public MySocket(Socket socket)
        {
            _socket = socket;
        }

        public virtual void Shutdown(SocketShutdown socketShutdown)
        {
            _socket.Shutdown(socketShutdown);
        }

        public virtual void Close()
        {
            _socket.Close();
        }

        public virtual bool ReceiveAsync(SocketAsyncEventArgs arg)
        {
            return _socket.ReceiveAsync(arg);
        }

        public virtual bool SendAsync(SocketAsyncEventArgs arg)
        {
            return _socket.SendAsync(arg);
        }

        public virtual bool DisconnectAsync(SocketAsyncEventArgs arg)
        {
            return _socket.DisconnectAsync(arg);
        }

        public SocketAwaitable ReceiveAsync(SocketAwaitable awaitable)
        {
            return DoInvoke(_socket.ReceiveAsync, awaitable);
        }

        public SocketAwaitable SendAsync(SocketAwaitable awaitable)
        {
            return DoInvoke(_socket.SendAsync, awaitable);
        }

        public SocketAwaitable DisconnectAsync(SocketAwaitable awaitable)
        {
            return DoInvoke(_socket.DisconnectAsync, awaitable);
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
