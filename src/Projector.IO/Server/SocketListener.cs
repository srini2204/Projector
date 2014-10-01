using Projector.IO.SocketHelpers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Server
{

    public class SocketListener
    {
        #region Private fields
        private int _numberOfAcceptedSockets;

        private BufferManager _theBufferManager;

        private Socket _listenSocket;

        private SemaphoreSlim _theMaxConnectionsEnforcer;

        private SocketListenerSettings _socketListenerSettings;

        private PrefixHandler _prefixHandler;
        private MessageHandler _messageHandler;


        private ObjectPool<SocketAwaitable> _poolOfAcceptSocketAwaitables;
        private ObjectPool<SocketAwaitable> _poolOfRecSendSocketAwaitables;
        #endregion

        #region Events
        public event EventHandler<ClientConnectedEventArgs> OnClientConnected;

        protected virtual void NotifyClientConnected(Socket socket)
        {
            var handler = OnClientConnected;
            if (handler != null)
            {
                handler(this, new ClientConnectedEventArgs(socket));
            }
        }

        public event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;

        protected virtual void NotifyClientDiconnected(IPEndPoint endPoint)
        {
            var handler = OnClientDisconnected;
            if (handler != null)
            {
                handler(this, new ClientDisconnectedEventArgs(endPoint));
            }
        }

        public event EventHandler<RequestReceivedEventArgs> OnRequestReceived;

        protected virtual void NotifyRequestReceived(SocketAwaitable socketAwaitable)
        {
            var handler = OnRequestReceived;
            if (handler != null)
            {
                handler(this, new RequestReceivedEventArgs(socketAwaitable));
            }
        }
        #endregion

        #region Constructor
        public SocketListener(SocketListenerSettings theSocketListenerSettings)
        {

            _numberOfAcceptedSockets = 0; //for testing
            _socketListenerSettings = theSocketListenerSettings;
            _prefixHandler = new PrefixHandler();
            _messageHandler = new MessageHandler();

            _theBufferManager = new BufferManager(_socketListenerSettings.BufferSize * _socketListenerSettings.NumberOfSaeaForRecSend * _socketListenerSettings.OpsToPreAllocate,
            _socketListenerSettings.BufferSize * _socketListenerSettings.OpsToPreAllocate);

            _poolOfRecSendSocketAwaitables = new ObjectPool<SocketAwaitable>(_socketListenerSettings.NumberOfSaeaForRecSend);
            _poolOfAcceptSocketAwaitables = new ObjectPool<SocketAwaitable>(_socketListenerSettings.MaxAcceptOps);

            // Create connections count enforcer
            _theMaxConnectionsEnforcer = new SemaphoreSlim(_socketListenerSettings.MaxConnections, _socketListenerSettings.MaxConnections);

            Init();
        }
        #endregion

        internal void Init()
        {
            for (var i = 0; i < _socketListenerSettings.MaxAcceptOps; i++)
            {
                _poolOfAcceptSocketAwaitables.Push(CreateNewSaeaForAccept(_poolOfAcceptSocketAwaitables));
            }




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

        internal SocketAwaitable CreateNewSaeaForAccept(ObjectPool<SocketAwaitable> pool)
        {
            return new SocketAwaitable(new SocketAsyncEventArgs());
        }

        public async Task StartListen()
        {
            _listenSocket = new Socket(_socketListenerSettings.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _listenSocket.Bind(_socketListenerSettings.LocalEndPoint);

            _listenSocket.Listen(_socketListenerSettings.Backlog);

            while (true)
            {
                SocketAwaitable acceptSocketAwaitable;

                if (_poolOfAcceptSocketAwaitables.Count > 1)
                {
                    try
                    {
                        acceptSocketAwaitable = _poolOfAcceptSocketAwaitables.Pop();
                    }
                    //or make a new one.
                    catch
                    {
                        acceptSocketAwaitable = CreateNewSaeaForAccept(_poolOfAcceptSocketAwaitables);
                    }
                }
                //or make a new one.
                else
                {
                    acceptSocketAwaitable = CreateNewSaeaForAccept(_poolOfAcceptSocketAwaitables);
                }

                await _theMaxConnectionsEnforcer.WaitAsync();

                await _listenSocket.AcceptAsync(acceptSocketAwaitable);

                if (acceptSocketAwaitable.EventArgs.SocketError != SocketError.Success)
                {
                    //Let's destroy this socket, since it could be bad.
                    HandleBadAccept(acceptSocketAwaitable);

                    continue;
                }

                NotifyClientConnected(acceptSocketAwaitable.EventArgs.AcceptSocket);

                Interlocked.Increment(ref _numberOfAcceptedSockets);


                // Get a SocketAsyncEventArgs object from the pool of receive/send op 
                //SocketAsyncEventArgs objects
                var receiveSendSocketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

                //Create sessionId in UserToken.
                ((DataHoldingUserToken)receiveSendSocketAwaitable.EventArgs.UserToken).CreateSessionId();

                //A new socket was created by the AcceptAsync method. The 
                //SocketAsyncEventArgs object which did the accept operation has that 
                //socket info in its AcceptSocket property. Now we will give
                //a reference for that socket to the SocketAsyncEventArgs 
                //object which will do receive/send.
                receiveSendSocketAwaitable.EventArgs.AcceptSocket = acceptSocketAwaitable.EventArgs.AcceptSocket;



                //We have handed off the connection info from the
                //accepting socket to the receiving socket. So, now we can
                //put the SocketAsyncEventArgs object that did the accept operation 
                //back in the pool for them. But first we will clear 
                //the socket info from that object, so it will be 
                //ready for a new socket when it comes out of the pool.
                acceptSocketAwaitable.EventArgs.AcceptSocket = null;
                _poolOfAcceptSocketAwaitables.Push(acceptSocketAwaitable);

                Task.Run(async () => await StartReceive(receiveSendSocketAwaitable)).ConfigureAwait(false);
            }
        }

        private async Task StartReceive(SocketAwaitable receiveSendSocketAwaitable)
        {
            var receiveSendEventArgs = receiveSendSocketAwaitable.EventArgs;
            var receiveSendToken = (DataHoldingUserToken)receiveSendEventArgs.UserToken;

            //Set the buffer for the receive operation.
            receiveSendEventArgs.SetBuffer(receiveSendToken.bufferOffset, _socketListenerSettings.BufferSize);

            while (true)
            {
                await receiveSendEventArgs.AcceptSocket.ReceiveAsync(receiveSendSocketAwaitable);

                if (receiveSendEventArgs.SocketError != SocketError.Success || receiveSendEventArgs.BytesTransferred == 0)
                {
                    receiveSendToken.Reset();
                    CloseClientSocket(receiveSendSocketAwaitable);

                    //Jump out of the ProcessReceive method.
                    return;
                }

                var remainingBytesToProcess = receiveSendEventArgs.BytesTransferred;

                //If we have not got all of the prefix already, 
                //then we need to work on it here.
                if (receiveSendToken.receivedPrefixBytesDoneCount < _socketListenerSettings.PrefixLength)
                {
                    remainingBytesToProcess = _prefixHandler.HandlePrefix(receiveSendEventArgs, receiveSendToken, remainingBytesToProcess);

                    if (remainingBytesToProcess == 0)
                    {
                        // We need to do another receive op, since we do not have
                        // the message yet, but remainingBytesToProcess == 0.
                        continue;
                    }
                }

                // If we have processed the prefix, we can work on the message now.
                // We'll arrive here when we have received enough bytes to read
                // the first byte after the prefix.
                var incomingTcpMessageIsReady = _messageHandler.HandleMessage(receiveSendEventArgs, receiveSendToken, remainingBytesToProcess);

                if (incomingTcpMessageIsReady == true)
                {

                    NotifyRequestReceived(receiveSendSocketAwaitable);

                    // Create a new DataHolder for next message.
                    receiveSendToken.CreateNewDataHolder();

                    //Reset the variables in the UserToken, to be ready for the
                    //next message that will be received on the socket in this
                    //SAEA object.
                    receiveSendToken.Reset();

                    //receiveSendToken.theMediator.PrepareOutgoingData();
                    //await StartSend(receiveSendSocketAwaitable);
                }
                else
                {
                    // Since we have NOT gotten enough bytes for the whole message,
                    // we need to do another receive op. Reset some variables first.

                    // All of the data that we receive in the next receive op will be
                    // message. None of it will be prefix. So, we need to move the 
                    // receiveSendToken.receiveMessageOffset to the beginning of the 
                    // receive buffer space for this SAEA.
                    receiveSendToken.receiveMessageOffset = receiveSendToken.bufferOffset;

                    // Do NOT reset receiveSendToken.receivedPrefixBytesDoneCount here.
                    // Just reset recPrefixBytesDoneThisOp.
                    receiveSendToken.recPrefixBytesDoneThisOp = 0;
                }
            }
        }

        public async Task SendAsync(Socket socket, byte[] data)
        {
            var sendSocketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

            sendSocketAwaitable.EventArgs.AcceptSocket = socket;

            var socketEventArgs = sendSocketAwaitable.EventArgs;
            var userToken = (DataHoldingUserToken)socketEventArgs.UserToken;

            userToken.dataToSend = data;
            userToken.sendBytesRemainingCount = data.Length;
            do
            {

                //The number of bytes to send depends on whether the message is larger than
                //the buffer or not. If it is larger than the buffer, then we will have
                //to post more than one send operation. If it is less than or equal to the
                //size of the send buffer, then we can accomplish it in one send op.
                if (userToken.sendBytesRemainingCount <= _socketListenerSettings.BufferSize)
                {
                    socketEventArgs.SetBuffer(userToken.bufferOffset, userToken.sendBytesRemainingCount);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(userToken.dataToSend, userToken.bytesSentAlreadyCount, socketEventArgs.Buffer, userToken.bufferOffset, userToken.sendBytesRemainingCount);
                }
                else
                {
                    //We cannot try to set the buffer any larger than its size.
                    //So since receiveSendToken.sendBytesRemainingCount > BufferSize, we just
                    //set it to the maximum size, to send the most data possible.
                    socketEventArgs.SetBuffer(userToken.bufferOffset, _socketListenerSettings.BufferSize);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(userToken.dataToSend, userToken.bytesSentAlreadyCount, socketEventArgs.Buffer, userToken.bufferOffset, _socketListenerSettings.BufferSize);

                    //We'll change the value of sendUserToken.sendBytesRemainingCount
                    //in the ProcessSend method.
                }

                //post asynchronous send operation
                await socketEventArgs.AcceptSocket.SendAsync(sendSocketAwaitable);

                if (socketEventArgs.SocketError == SocketError.Success)
                {
                    userToken.sendBytesRemainingCount = userToken.sendBytesRemainingCount - socketEventArgs.BytesTransferred;

                    if (userToken.sendBytesRemainingCount != 0)
                    {
                        // If some of the bytes in the message have NOT been sent,
                        // then we will need to post another send operation, after we store
                        // a count of how many bytes that we sent in this send op.
                        userToken.bytesSentAlreadyCount += socketEventArgs.BytesTransferred;
                        // So let's loop back to StartSend().
                        continue;
                    }
                }
                else
                {
                    // We'll just close the socket if there was a
                    // socket error when receiving data from the client.
                    userToken.Reset();
                    CloseClientSocket(sendSocketAwaitable);
                }
            }
            while (userToken.sendBytesRemainingCount != 0);

            sendSocketAwaitable.EventArgs.AcceptSocket = null;
            _poolOfRecSendSocketAwaitables.Push(sendSocketAwaitable);
        }

        private void CloseClientSocket(SocketAwaitable socketAwaitable)
        {

            var eventArgs = socketAwaitable.EventArgs;
            var endPoint = (IPEndPoint)eventArgs.AcceptSocket.RemoteEndPoint;
            var receiveSendToken = (eventArgs.UserToken as DataHoldingUserToken);

            // do a shutdown before you close the socket
            try
            {
                eventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            // throws if socket was already closed
            catch (SocketException)
            {

            }

            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            eventArgs.AcceptSocket.Close();

            //Make sure the new DataHolder has been created for the next connection.
            //If it has, then dataMessageReceived should be null.
            if (receiveSendToken.theDataHolder.dataMessageReceived != null)
            {
                receiveSendToken.CreateNewDataHolder();
            }

            _poolOfRecSendSocketAwaitables.Push(socketAwaitable);

            Interlocked.Decrement(ref _numberOfAcceptedSockets);

            _theMaxConnectionsEnforcer.Release();

            NotifyClientDiconnected(endPoint);
        }

        private void HandleBadAccept(SocketAwaitable socketAwaitable)
        {
            var acceptEventArgs = socketAwaitable.EventArgs;
            acceptEventArgs.AcceptSocket.Close();

            _poolOfAcceptSocketAwaitables.Push(socketAwaitable);
        }

        internal void CleanUpOnExit()
        {
            DisposeAllSaeaObjects();
        }

        private void DisposeAllSaeaObjects()
        {
            SocketAsyncEventArgs eventArgs;
            while (_poolOfAcceptSocketAwaitables.Count > 0)
            {
                eventArgs = _poolOfAcceptSocketAwaitables.Pop().EventArgs;
                eventArgs.Dispose();
            }
            while (_poolOfRecSendSocketAwaitables.Count > 0)
            {
                eventArgs = _poolOfRecSendSocketAwaitables.Pop().EventArgs;
                eventArgs.Dispose();
            }
        }
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public ClientConnectedEventArgs(Socket socket)
        {
            EndPoint = (IPEndPoint)socket.RemoteEndPoint;
            Socket = socket;
        }
        public IPEndPoint EndPoint { get; private set; }
        public Socket Socket { get; private set; }
    }

    public class ClientDisconnectedEventArgs : EventArgs
    {
        public ClientDisconnectedEventArgs(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
        public IPEndPoint EndPoint { get; private set; }
    }

    public class RequestReceivedEventArgs : EventArgs
    {
        public RequestReceivedEventArgs(SocketAwaitable socketAwaitable)
        {
            EndPoint = (IPEndPoint)socketAwaitable.EventArgs.AcceptSocket.RemoteEndPoint;
            SocketAwaitable = socketAwaitable;
        }
        public IPEndPoint EndPoint { get; private set; }
        public SocketAwaitable SocketAwaitable { get; private set; }
    }

}
