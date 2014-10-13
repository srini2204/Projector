using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.SocketHelpers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        private readonly ConcurrentDictionary<IPEndPoint, SocketWrapper> _clients = new ConcurrentDictionary<IPEndPoint, SocketWrapper>();

        private readonly ObjectPool<SocketAwaitable> _poolOfRecSendSocketAwaitables;

        private readonly SocketListenerSettings _socketListenerSettings;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogicalServer _logicalServer;

        public Server(SocketListenerSettings socketListenerSettings, ILogicalServer logicalServer)
        {
            _socketListenerSettings = socketListenerSettings;
            _logicalServer = logicalServer;

            _poolOfRecSendSocketAwaitables = new ObjectPool<SocketAwaitable>();
            _socketListener = new SocketListener();

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

                if (socket != null)
                {
                    StatClientServing(socket);
                }
            }

            var taskList = new List<Task>();
            foreach (var socket in _clients)
            {
                taskList.Add(socket.Value.DisconnectAsync());
            }

            await Task.WhenAll(taskList);

            while (!_clients.IsEmpty)
            {
                await Task.Delay(100);
            }
        }

        private async void StatClientServing(Socket socket)
        {
            await Task.Yield();

            SocketAwaitable awaitable1 = null;
            SocketAwaitable awaitable2 = null;

            try
            {
                awaitable1 = _poolOfRecSendSocketAwaitables.Pop();
                awaitable2 = _poolOfRecSendSocketAwaitables.Pop();

                var socketWrapper = new SocketWrapper(awaitable1, awaitable2, new MySocket(socket), _socketListenerSettings.BufferSize);
                await OnClientConnected((IPEndPoint)socket.RemoteEndPoint, socketWrapper);

                var token = _cancellationTokenSource.Token;
                var endPoint = (IPEndPoint)socket.RemoteEndPoint;
                var signaledForStopping = false;

                using (var inputStream = new MemoryStream())
                {
                    while (!token.IsCancellationRequested && !signaledForStopping)
                    {
                        signaledForStopping = !await socketWrapper.ReceiveAsync(inputStream);

                        if (!signaledForStopping)
                        {
                            signaledForStopping = !await _logicalServer.ProcessRequestAsync(socketWrapper, inputStream);
                        }
                    }
                }

                await OnClientDiconnected(endPoint);
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

        private Task OnClientDiconnected(IPEndPoint endPoint)
        {
            SocketWrapper socketWrapper;
            _clients.TryRemove(endPoint, out socketWrapper);
            return _logicalServer.ClientDiconnected(endPoint);
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
