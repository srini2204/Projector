using Projector.IO.SocketHelpers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Server
{

    public class SocketListener
    {
        #region Private fields

        private Socket _listenSocket;

        private SemaphoreSlim _theMaxConnectionsEnforcer;

        private SocketListenerSettings _socketListenerSettings;

        private readonly SocketAwaitable _acceptSocketAwaitable;
        #endregion

        #region Constructor
        public SocketListener(SocketListenerSettings theSocketListenerSettings)
        {

            _socketListenerSettings = theSocketListenerSettings;

            _acceptSocketAwaitable = new SocketAwaitable(new SocketAsyncEventArgs());

            // Create connections count enforcer
            _theMaxConnectionsEnforcer = new SemaphoreSlim(_socketListenerSettings.MaxConnections, _socketListenerSettings.MaxConnections);
        }
        #endregion




        public void StartListen()
        {
            _listenSocket = new Socket(_socketListenerSettings.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _listenSocket.Bind(_socketListenerSettings.LocalEndPoint);

            _listenSocket.Listen(_socketListenerSettings.Backlog);
        }

        public async Task<Socket> TakeNewClient()
        {
            do
            {
                await _theMaxConnectionsEnforcer.WaitAsync();

                await _listenSocket.AcceptAsync(_acceptSocketAwaitable);

                if (_acceptSocketAwaitable.EventArgs.SocketError != SocketError.Success)
                {
                    //Let's destroy this socket, since it could be bad.
                    _acceptSocketAwaitable.EventArgs.AcceptSocket.Close();
                }

            }
            while (_acceptSocketAwaitable.EventArgs.SocketError != SocketError.Success);


            var clientSocket = _acceptSocketAwaitable.EventArgs.AcceptSocket;
            _acceptSocketAwaitable.EventArgs.AcceptSocket = null;

            return clientSocket;
        }

        public void StopListen()
        {
            _listenSocket.Shutdown(SocketShutdown.Both);
            _listenSocket.Close();
        }

    }
}
