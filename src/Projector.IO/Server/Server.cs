using Projector.IO.Protocol.Responses;
using Projector.IO.SocketHelpers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Server
{
    public class Server
    {
        private readonly BufferManager _theBufferManager;

        private readonly SocketListener _socketListener;

        private readonly Dictionary<IPEndPoint, Socket> _clients = new Dictionary<IPEndPoint, Socket>();

        private readonly ObjectPool<SocketAwaitable> _poolOfRecSendSocketAwaitables;

        private readonly SocketListenerSettings _socketListenerSettings;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Server()
        {
            _socketListenerSettings = new SocketListenerSettings(10000, 1, 100, 4, 25, 10, new IPEndPoint(IPAddress.Any, 4444));
            _poolOfRecSendSocketAwaitables = new ObjectPool<SocketAwaitable>(_socketListenerSettings.NumberOfSaeaForRecSend);
            _socketListener = new SocketListener(_socketListenerSettings);

            _theBufferManager = new BufferManager(_socketListenerSettings.BufferSize * _socketListenerSettings.NumberOfSaeaForRecSend * _socketListenerSettings.OpsToPreAllocate,
            _socketListenerSettings.BufferSize * _socketListenerSettings.OpsToPreAllocate);

            Init();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        internal void Init()
        {
            for (var i = 0; i < _socketListenerSettings.NumberOfSaeaForRecSend; i++)
            {
                var eventArgObject = new SocketAsyncEventArgs();

                _theBufferManager.SetBuffer(eventArgObject);

                //We can store data in the UserToken property of SAEA object.
                var theTempReceiveSendUserToken = new DataHoldingUserToken(eventArgObject.Offset, _socketListenerSettings.PrefixLength);

                //We'll have an object that we call DataHolder, that we can remove from
                //the UserToken when we are finished with it. So, we can hang on to the
                //DataHolder, pass it to an app, serialize it, or whatever.
                theTempReceiveSendUserToken.CreateNewDataHolder();

                eventArgObject.UserToken = theTempReceiveSendUserToken;


                _poolOfRecSendSocketAwaitables.Push(new SocketAwaitable(eventArgObject));
            }
        }

        public async Task Start()
        {
            _socketListener.StartListen();
            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                var socket = await _socketListener.TakeNewClient();
                StatClientServing(socket);
            }
        }

        private async void StatClientServing(Socket socket)
        {
            await Task.Yield();
            var socketWrapper = new SocketWrapper(_poolOfRecSendSocketAwaitables, socket, 4, 25);

            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                var data = await socketWrapper.ReceiveAsync();
                await socketWrapper.SendAsync(new OkResponse().GetBytes());
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _socketListener.StopListen();

            // we can also wait for the clients here
        }

        internal void CleanUpOnExit()
        {
            DisposeAllSaeaObjects();
        }

        private void DisposeAllSaeaObjects()
        {
            while (_poolOfRecSendSocketAwaitables.Count > 0)
            {
                _poolOfRecSendSocketAwaitables.Pop().EventArgs.Dispose();
            }
        }
    }
}
