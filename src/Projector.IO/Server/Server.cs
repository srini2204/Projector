using Projector.IO.SocketHelpers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Server
{

    public class Server
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

        protected virtual void NotifyClientConnected(IPEndPoint endPoint)
        {
            var handler = OnClientConnected;
            if (handler != null)
            {
                handler(this, new ClientConnectedEventArgs(endPoint));
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

        protected virtual void NotifyRequestReceived(IPEndPoint endPoint)
        {
            var handler = OnRequestReceived;
            if (handler != null)
            {
                handler(this, new RequestReceivedEventArgs(endPoint));
            }
        }
        #endregion

        #region Constructor
        public Server(SocketListenerSettings theSocketListenerSettings)
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


            SocketAsyncEventArgs eventArgObjectForPool;

            int tokenId;

            for (var i = 0; i < _socketListenerSettings.NumberOfSaeaForRecSend; i++)
            {
                eventArgObjectForPool = new SocketAsyncEventArgs();

                _theBufferManager.SetBuffer(eventArgObjectForPool);

                tokenId = _poolOfRecSendSocketAwaitables.AssignTokenId() + 1000000;

                //We can store data in the UserToken property of SAEA object.
                var theTempReceiveSendUserToken = new DataHoldingUserToken(eventArgObjectForPool, eventArgObjectForPool.Offset, eventArgObjectForPool.Offset + _socketListenerSettings.BufferSize, _socketListenerSettings.ReceivePrefixLength, _socketListenerSettings.SendPrefixLength, tokenId);

                //We'll have an object that we call DataHolder, that we can remove from
                //the UserToken when we are finished with it. So, we can hang on to the
                //DataHolder, pass it to an app, serialize it, or whatever.
                theTempReceiveSendUserToken.CreateNewDataHolder();

                eventArgObjectForPool.UserToken = theTempReceiveSendUserToken;


                _poolOfRecSendSocketAwaitables.Push(new SocketAwaitable(eventArgObjectForPool));
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

                NotifyClientConnected(((IPEndPoint)acceptSocketAwaitable.EventArgs.AcceptSocket.RemoteEndPoint));

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
            receiveSendEventArgs.SetBuffer(receiveSendToken.bufferOffsetReceive, _socketListenerSettings.BufferSize);

            while (true)
            {
                await receiveSendEventArgs.AcceptSocket.ReceiveAsync(receiveSendSocketAwaitable);

                if (receiveSendEventArgs.SocketError != SocketError.Success
                    || receiveSendEventArgs.BytesTransferred == 0)
                {
                    receiveSendToken.Reset();
                    CloseClientSocket(receiveSendSocketAwaitable);

                    //Jump out of the ProcessReceive method.
                    return;
                }

                var remainingBytesToProcess = receiveSendEventArgs.BytesTransferred;

                //If we have not got all of the prefix already, 
                //then we need to work on it here.
                if (receiveSendToken.receivedPrefixBytesDoneCount < _socketListenerSettings.ReceivePrefixLength)
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
                    NotifyRequestReceived((IPEndPoint)receiveSendEventArgs.AcceptSocket.RemoteEndPoint);

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
                    receiveSendToken.receiveMessageOffset = receiveSendToken.bufferOffsetReceive;

                    // Do NOT reset receiveSendToken.receivedPrefixBytesDoneCount here.
                    // Just reset recPrefixBytesDoneThisOp.
                    receiveSendToken.recPrefixBytesDoneThisOp = 0;
                }
            }
        }

        private async Task StartSend(SocketAwaitable socketAwaitable)
        {
            while (true)
            {
                var receiveSendEventArgs = socketAwaitable.EventArgs;
                var receiveSendToken = (DataHoldingUserToken)receiveSendEventArgs.UserToken;

                //Set the buffer. You can see on Microsoft's page at 
                //http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.setbuffer.aspx
                //that there are two overloads. One of the overloads has 3 parameters.
                //When setting the buffer, you need 3 parameters the first time you set it,
                //which we did in the Init method. The first of the three parameters
                //tells what byte array to use as the buffer. After we tell what byte array
                //to use we do not need to use the overload with 3 parameters any more.
                //(That is the whole reason for using the buffer block. You keep the same
                //byte array as buffer always, and keep it all in one block.)
                //Now we use the overload with two parameters. We tell 
                // (1) the offset and
                // (2) the number of bytes to use, starting at the offset.

                //The number of bytes to send depends on whether the message is larger than
                //the buffer or not. If it is larger than the buffer, then we will have
                //to post more than one send operation. If it is less than or equal to the
                //size of the send buffer, then we can accomplish it in one send op.
                if (receiveSendToken.sendBytesRemainingCount <= _socketListenerSettings.BufferSize)
                {
                    receiveSendEventArgs.SetBuffer(receiveSendToken.bufferOffsetSend, receiveSendToken.sendBytesRemainingCount);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(receiveSendToken.dataToSend, receiveSendToken.bytesSentAlreadyCount, receiveSendEventArgs.Buffer, receiveSendToken.bufferOffsetSend, receiveSendToken.sendBytesRemainingCount);
                }
                else
                {
                    //We cannot try to set the buffer any larger than its size.
                    //So since receiveSendToken.sendBytesRemainingCount > BufferSize, we just
                    //set it to the maximum size, to send the most data possible.
                    receiveSendEventArgs.SetBuffer(receiveSendToken.bufferOffsetSend, _socketListenerSettings.BufferSize);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(receiveSendToken.dataToSend, receiveSendToken.bytesSentAlreadyCount, receiveSendEventArgs.Buffer, receiveSendToken.bufferOffsetSend, _socketListenerSettings.BufferSize);

                    //We'll change the value of sendUserToken.sendBytesRemainingCount
                    //in the ProcessSend method.
                }

                //post asynchronous send operation
                await receiveSendEventArgs.AcceptSocket.SendAsync(socketAwaitable);

                if (receiveSendEventArgs.SocketError == SocketError.Success)
                {
                    receiveSendToken.sendBytesRemainingCount = receiveSendToken.sendBytesRemainingCount - receiveSendEventArgs.BytesTransferred;

                    if (receiveSendToken.sendBytesRemainingCount != 0)
                    {
                        // If some of the bytes in the message have NOT been sent,
                        // then we will need to post another send operation, after we store
                        // a count of how many bytes that we sent in this send op.
                        receiveSendToken.bytesSentAlreadyCount += receiveSendEventArgs.BytesTransferred;
                        // So let's loop back to StartSend().
                        continue;
                    }
                }
                else
                {
                    // We'll just close the socket if there was a
                    // socket error when receiving data from the client.
                    receiveSendToken.Reset();
                    CloseClientSocket(socketAwaitable);
                }
            }
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
        public ClientConnectedEventArgs(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
        public IPEndPoint EndPoint { get; private set; }
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
        public RequestReceivedEventArgs(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
        public IPEndPoint EndPoint { get; private set; }
    }

}
