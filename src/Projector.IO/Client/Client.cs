using Projector.IO.SocketHelpers;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Client
{
    public class Client
    {
        private readonly BufferManager _theBufferManager;

        private readonly SocketConnector _socketConnector;

        private SocketAwaitable _sendSocketAwaitable;

        private SocketAwaitable _receiveSocketAwaitable;

        private readonly SocketClientSettings _socketClientSettings;

        private SocketWrapper _socketWrapper;

        public Client(SocketClientSettings socketClientSettings)
        {
            _socketClientSettings = socketClientSettings;
            _socketConnector = new SocketConnector();

            _theBufferManager = new BufferManager(_socketClientSettings.BufferSize * 2, _socketClientSettings.BufferSize);

            _sendSocketAwaitable = GetSocketAwaitable();
            _receiveSocketAwaitable = GetSocketAwaitable();
        }

        private SocketAwaitable GetSocketAwaitable()
        {
            var eventArgObject = new SocketAsyncEventArgs();

            _theBufferManager.SetBuffer(eventArgObject);

            //We can store data in the UserToken property of SAEA object.
            eventArgObject.UserToken = new DataHoldingUserToken(eventArgObject.Offset);

            return new SocketAwaitable(eventArgObject);
        }

        public async Task SendCommand(Stream stream)
        {
            await _socketWrapper.SendAsync(stream);
        }

        public async Task ConnectAsync()
        {
            var socket = await _socketConnector.ConnectAsync(_socketClientSettings.ServerEndPoint);

            _socketWrapper = new SocketWrapper(_sendSocketAwaitable, _receiveSocketAwaitable, new MySocket(socket), _socketClientSettings.BufferSize);
        }

        public async Task<bool> ReadAsync(Stream stream)
        {
            var success = await _socketWrapper.ReceiveAsync(stream);
            if (!success)
            {
                NotifyClientDiconnected();
            }
            return success;
        }

        public Task DisconnectAsync()
        {
            return _socketWrapper.DisconnectAsync();
        }

        #region Events

        public event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;

        protected virtual void NotifyClientDiconnected()
        {
            var handler = OnClientDisconnected;
            if (handler != null)
            {
                handler(this, new ClientDisconnectedEventArgs());
            }
        }

        #endregion

        public class ClientDisconnectedEventArgs : EventArgs
        {

        }
    }
}
