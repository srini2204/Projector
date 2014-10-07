using Projector.IO.Protocol.Commands;
using Projector.IO.SocketHelpers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Client
{
    public class Client
    {
        private readonly BufferManager _theBufferManager;

        private readonly SocketConnector _socketConnector;

        private readonly ObjectPool<SocketAwaitable> _poolOfRecSendSocketAwaitables;

        private readonly SocketClientSettings _socketClientSettings;

        private SocketWrapper _socketWrapper;

        private AutoResetEvent _callSync = new AutoResetEvent(false);

        public Client()
        {
            _socketClientSettings = new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 25, 10);
            _socketConnector = new SocketConnector();
            _poolOfRecSendSocketAwaitables = new ObjectPool<SocketAwaitable>();

            _theBufferManager = new BufferManager(_socketClientSettings.BufferSize * _socketClientSettings.OpsToPreAllocate, _socketClientSettings.BufferSize);

            Init();

        }

        internal void Init()
        {
            for (var i = 0; i < _socketClientSettings.OpsToPreAllocate; i++)
            {
                var eventArgObject = new SocketAsyncEventArgs();

                _theBufferManager.SetBuffer(eventArgObject);

                //We can store data in the UserToken property of SAEA object.
                var theTempReceiveSendUserToken = new DataHoldingUserToken(eventArgObject.Offset, _socketClientSettings.PrefixLength);

                //We'll have an object that we call DataHolder, that we can remove from
                //the UserToken when we are finished with it. So, we can hang on to the
                //DataHolder, pass it to an app, serialize it, or whatever.
                theTempReceiveSendUserToken.CreateNewDataHolder();

                eventArgObject.UserToken = theTempReceiveSendUserToken;


                _poolOfRecSendSocketAwaitables.Push(new SocketAwaitable(eventArgObject));
            }
        }

        public async Task SendCommand(ICommand command)
        {

            await _socketWrapper.SendAsync(command.GetBytes());

            _callSync.WaitOne();

        }

        public async Task ConnectAsync()
        {
            var socket = await _socketConnector.ConnectAsync(_socketClientSettings.ServerEndPoint);

            _socketWrapper = new SocketWrapper(_poolOfRecSendSocketAwaitables, socket, _socketClientSettings.PrefixLength, _socketClientSettings.BufferSize);


            StartReadingMessages();
        }

        private async void StartReadingMessages()
        {
            await Task.Yield();

            while (true)
            {
                var data = await _socketWrapper.ReceiveAsync();
                if (data == null)
                {
                    NotifyClientDiconnected();
                    return;
                }

                if (data.Length > 0)
                {
                    _callSync.Set();
                }

            }


        }

        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
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
