using Projector.IO.SocketHelpers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Client
{
    public sealed class SocketConnector
    {
        private readonly SocketClientSettings _socketClientSettings;

        public SocketConnector(SocketClientSettings theSocketClientSettings)
        {
            _socketClientSettings = theSocketClientSettings;
        }

        public async Task<Socket> ConnectAsync()
        {
            var socketAsyncEventArgs = new SocketAsyncEventArgs();

            socketAsyncEventArgs.RemoteEndPoint = _socketClientSettings.ServerEndPoint;

            socketAsyncEventArgs.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connectSocketAwaitable = new SocketAwaitable(socketAsyncEventArgs);
            await socketAsyncEventArgs.AcceptSocket.ConnectAsync(connectSocketAwaitable);

            if (socketAsyncEventArgs.SocketError == SocketError.Success)
            {
                return socketAsyncEventArgs.AcceptSocket;
            }
            else if ((socketAsyncEventArgs.SocketError != SocketError.ConnectionRefused)
                && (socketAsyncEventArgs.SocketError != SocketError.TimedOut)
                && (socketAsyncEventArgs.SocketError != SocketError.HostUnreachable))
            {
                CloseSocket(socketAsyncEventArgs.AcceptSocket);
            }

            throw new SocketException((int)socketAsyncEventArgs.SocketError);
        }


        private static void CloseSocket(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            socket.Close();
        }
    }
}