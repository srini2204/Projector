using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.SocketHelpers
{
    public class MySocket : ISocket
    {
        private readonly Socket _socket;

        public MySocket(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
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


        public Task ReceiveAsync(SocketAwaitable awaitable)
        {
            return DoInvoke(_socket.ReceiveAsync, awaitable);
        }

        public Task SendAsync(SocketAwaitable awaitable)
        {
            return DoInvoke(_socket.SendAsync, awaitable);
        }

        public Task DisconnectAsync(SocketAwaitable awaitable)
        {
            return DoInvoke(_socket.DisconnectAsync, awaitable);
        }

        private async static Task DoInvoke(Func<SocketAsyncEventArgs, bool> socketAsyncFunc, SocketAwaitable awaitable)
        {
            if (awaitable == null)
            {
                throw new ArgumentNullException("awaitable");
            }

            awaitable.Reset();
            if (!socketAsyncFunc(awaitable.EventArgs))
            {
                awaitable.WasCompleted = true;
            }

            await awaitable;
            awaitable.BytesTransferred = awaitable.EventArgs.BytesTransferred;
        }


        public EndPoint RemoteEndPoint
        {
            get { return _socket.RemoteEndPoint; }
        }
    }
}
