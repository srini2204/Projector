using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.SocketHelpers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Server
{
    public class Server
    {
        private readonly BufferManager _theBufferManager;

        private readonly ISocketListener _socketListener;

        private readonly ConcurrentDictionary<IPEndPoint, SocketWrapper> _clients = new ConcurrentDictionary<IPEndPoint, SocketWrapper>();

        private readonly ObjectPool<SocketAwaitable> _poolOfRecSendSocketAwaitables;

        private readonly SocketListenerSettings _socketListenerSettings;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogicalServer _logicalServer;

        public Server(SocketListenerSettings socketListenerSettings, ILogicalServer logicalServer, ISocketListener socketListenere)
        {
            _socketListenerSettings = socketListenerSettings;
            _logicalServer = logicalServer;

            _poolOfRecSendSocketAwaitables = new ObjectPool<SocketAwaitable>();
            _socketListener = socketListenere;

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
                eventArgObject.UserToken = new DataHoldingUserToken(eventArgObject.Offset);

                _poolOfRecSendSocketAwaitables.Push(new SocketAwaitable(eventArgObject));
            }
        }

        public async Task Start()
        {
            _socketListener.StartListen(_socketListenerSettings.LocalEndPoint, _socketListenerSettings.Backlog);
            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                var socket = await _socketListener.TakeNewClient();

                if (socket == null)
                {
                    break; // notified for stopping
                }

                StartClientServing(socket);
            }

            await _logicalServer.Stop();
        }

        private async void StartClientServing(ISocket socket)
        {
            await Task.Yield();

            SocketAwaitable awaitable1 = null;
            SocketAwaitable awaitable2 = null;

            try
            {
                awaitable1 = _poolOfRecSendSocketAwaitables.Pop();
                awaitable2 = _poolOfRecSendSocketAwaitables.Pop();

                var socketWrapper = new SocketWrapper(awaitable1, awaitable2, socket, _socketListenerSettings.BufferSize);
                await OnClientConnected((IPEndPoint)socket.RemoteEndPoint, socketWrapper);


            }
            finally
            {
                if (awaitable1 != null)
                {
                    _poolOfRecSendSocketAwaitables.Push(awaitable1);
                }

                if (awaitable2 != null)
                {
                    _poolOfRecSendSocketAwaitables.Push(awaitable2);
                }
            }
        }



        private Task OnClientConnected(IPEndPoint endPoint, SocketWrapper socketWrapper)
        {
            _clients.TryAdd(endPoint, socketWrapper);
            return _logicalServer.RegisterConnectedClient(endPoint, socketWrapper);
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
