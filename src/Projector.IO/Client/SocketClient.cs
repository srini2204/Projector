using Projector.IO.SocketHelpers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Client
{
    public sealed class SocketClient
    {

        private readonly SocketClientSettings _socketClientSettings;

        private SocketAsyncEventArgs _receiveSendEventArgs;
        private SocketAwaitable _socketAwaitable;




        public SocketClient(SocketClientSettings theSocketClientSettings)
        {
            _socketClientSettings = theSocketClientSettings;
        }

        public async Task ConnectAsync()
        {
            var socketAsyncEventArgs = new SocketAsyncEventArgs();


            socketAsyncEventArgs.RemoteEndPoint = _socketClientSettings.ServerEndPoint;

            socketAsyncEventArgs.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connectSocketAwaitable = new SocketAwaitable(socketAsyncEventArgs);
            await socketAsyncEventArgs.AcceptSocket.ConnectAsync(connectSocketAwaitable);

            if (socketAsyncEventArgs.SocketError == SocketError.Success)
            {
                _receiveSendEventArgs = new SocketAsyncEventArgs();
                _socketAwaitable = new SocketAwaitable(_receiveSendEventArgs);

                _receiveSendEventArgs.SetBuffer(new byte[_socketClientSettings.BufferSize], 0, _socketClientSettings.BufferSize);

                var receiveSendToken = new DataHoldingUserToken(_receiveSendEventArgs.Offset, _socketClientSettings.PrefixLength);
                receiveSendToken.CreateNewDataHolder();
                _receiveSendEventArgs.UserToken = receiveSendToken;
                _receiveSendEventArgs.AcceptSocket = socketAsyncEventArgs.AcceptSocket;

            }
            else if ((socketAsyncEventArgs.SocketError != SocketError.ConnectionRefused)
                && (socketAsyncEventArgs.SocketError != SocketError.TimedOut)
                && (socketAsyncEventArgs.SocketError != SocketError.HostUnreachable))
            {
                CloseSocket(socketAsyncEventArgs.AcceptSocket);
            }
        }


        public async Task DisconnectAsync()
        {
            var receiveSendToken = (DataHoldingUserToken)_receiveSendEventArgs.UserToken;


            _receiveSendEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);

            await _receiveSendEventArgs.AcceptSocket.DisconnectAsync(_socketAwaitable);

            if (_receiveSendEventArgs.SocketError != SocketError.Success)
            {

            }

            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            _receiveSendEventArgs.AcceptSocket.Close();


            //create an object that we can write data to.
            receiveSendToken.CreateNewDataHolder();
        }


        private void CloseSocket(Socket theSocket)
        {
            try
            {
                theSocket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            theSocket.Close();
        }
    }
}