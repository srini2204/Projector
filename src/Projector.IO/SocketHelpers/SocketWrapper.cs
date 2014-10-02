using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.SocketHelpers
{
    class SocketWrapper
    {
        private readonly ObjectPool<SocketAwaitable> _poolOfRecSendSocketAwaitables;
        private readonly Socket _socket;
        private readonly int _bufferSize;
        private readonly int _prefixLength;

        public SocketWrapper(ObjectPool<SocketAwaitable> poolOfRecSendSocketAwaitables, Socket socket, int prefixLength, int bufferSize)
        {
            _poolOfRecSendSocketAwaitables = poolOfRecSendSocketAwaitables;
            _socket = socket;
            _bufferSize = bufferSize;
            _prefixLength = bufferSize;
        }

        public async Task SendAsync(byte[] data)
        {
            var sendSocketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

            sendSocketAwaitable.EventArgs.AcceptSocket = _socket;

            var sendEventArgs = sendSocketAwaitable.EventArgs;

            DataHoldingUserToken receiveSendToken = (DataHoldingUserToken)sendEventArgs.UserToken;
            receiveSendToken.sendBytesRemainingCount = data.Length;
            do
            {

                if (receiveSendToken.sendBytesRemainingCount <= _bufferSize)
                {
                    sendEventArgs.SetBuffer(receiveSendToken.bufferOffset, receiveSendToken.sendBytesRemainingCount);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(data, receiveSendToken.bytesSentAlreadyCount, sendEventArgs.Buffer, receiveSendToken.bufferOffset, receiveSendToken.sendBytesRemainingCount);
                }
                else
                {
                    //We cannot try to set the buffer any larger than its size.
                    //So since receiveSendToken.sendBytesRemaining > its size, we just
                    //set it to the maximum size, to send the most data possible.
                    sendEventArgs.SetBuffer(receiveSendToken.bufferOffset, _bufferSize);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(data, receiveSendToken.bytesSentAlreadyCount, sendEventArgs.Buffer, receiveSendToken.bufferOffset, _bufferSize);

                    //We'll change the value of sendUserToken.sendBytesRemaining
                    //in the ProcessSend method.
                }

                await sendEventArgs.AcceptSocket.SendAsync(sendSocketAwaitable);



                if (sendEventArgs.SocketError == SocketError.Success)
                {
                    receiveSendToken.sendBytesRemainingCount = receiveSendToken.sendBytesRemainingCount - sendEventArgs.BytesTransferred;


                }
                else
                {
                    // We'll just close the socket if there was a
                    // socket error when receiving data from the client.
                    receiveSendToken.Reset();
                    await DisconnectAsync();
                }

                // If this if statement is true, then we have sent all of the
                // bytes in the message. Otherwise, at least one more send
                // operation will be required to send the data.
            }
            while (receiveSendToken.sendBytesRemainingCount != 0);

            sendSocketAwaitable.EventArgs.AcceptSocket = null;
            _poolOfRecSendSocketAwaitables.Push(sendSocketAwaitable);
        }


        public async Task<byte[]> ReceiveAsync()
        {
            var socketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

            socketAwaitable.EventArgs.AcceptSocket = _socket;

            var eventArgs = socketAwaitable.EventArgs;

            bool incomingTcpMessageIsReady = false;
            DataHoldingUserToken receiveSendToken = (DataHoldingUserToken)eventArgs.UserToken;

            do
            {
                //Set buffer for receive.
                eventArgs.SetBuffer(receiveSendToken.bufferOffset, _bufferSize);

                await eventArgs.AcceptSocket.ReceiveAsync(socketAwaitable);

                // If there was a socket error, close the connection.
                if (eventArgs.SocketError != SocketError.Success)
                {
                    receiveSendToken.Reset();
                    await DisconnectAsync();
                    return new byte[] { }; // exception?
                }

                //If no data was received, close the connection.
                if (eventArgs.BytesTransferred == 0)
                {
                    receiveSendToken.Reset();
                    await DisconnectAsync();
                    return new byte[] { }; // exception?
                }

                var remainingBytesToProcess = eventArgs.BytesTransferred;


                // If we have not got all of the prefix then we need to work on it. 
                // receivedPrefixBytesDoneCount tells us how many prefix bytes were
                // processed during previous receive ops which contained data for 
                // this message. (In normal use, usually there will NOT have been any 
                // previous receive ops here. So receivedPrefixBytesDoneCount would be 0.)
                if (receiveSendToken.receivedPrefixBytesDoneCount < _prefixLength)
                {
                    remainingBytesToProcess = PrefixHandler.HandlePrefix(eventArgs, receiveSendToken, remainingBytesToProcess);

                    if (remainingBytesToProcess == 0)
                    {
                        // We need to do another receive op, since we do not have
                        // the message yet.


                        //Jump out of the method, since there is no more data.
                        continue;
                    }
                }

                // If we have processed the prefix, we can work on the message now.
                // We'll arrive here when we have received enough bytes to read
                // the first byte after the prefix.
                incomingTcpMessageIsReady = MessageHandler.HandleMessage(eventArgs, receiveSendToken, remainingBytesToProcess);

                if (incomingTcpMessageIsReady != true)
                {
                    // Since we have NOT gotten enough bytes for the whole message,
                    // we need to do another receive op. Reset some variables first.

                    // All of the data that we receive in the next receive op will be
                    // message. None of it will be prefix. So, we need to move the 
                    // receiveSendToken.receiveMessageOffset to the beginning of the 
                    // buffer space for this SAEA.
                    receiveSendToken.receiveMessageOffset = receiveSendToken.bufferOffset;

                    // Do NOT reset receiveSendToken.receivedPrefixBytesDoneCount here.
                    // Just reset recPrefixBytesDoneThisOp.
                    receiveSendToken.recPrefixBytesDoneThisOp = 0;


                }
            }
            while (!incomingTcpMessageIsReady);

            var resultBytes = receiveSendToken.theDataHolder.dataMessageReceived;

            //null out the byte array, for the next message
            receiveSendToken.theDataHolder.dataMessageReceived = null;

            //Reset the variables in the UserToken, to be ready for the
            //next message that will be received on the socket in this
            //SAEA object.
            receiveSendToken.Reset();

            socketAwaitable.EventArgs.AcceptSocket = null;
            _poolOfRecSendSocketAwaitables.Push(socketAwaitable);

            return resultBytes;

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

            //_theMaxConnectionsEnforcer.Release();
        }

        public async Task DisconnectAsync()
        {
            var socketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

            socketAwaitable.EventArgs.AcceptSocket = _socket;

            _socket.Shutdown(SocketShutdown.Both);

            await _socket.DisconnectAsync(socketAwaitable);

            if (socketAwaitable.EventArgs.SocketError != SocketError.Success)
            {

            }

            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            _socket.Close();

            _poolOfRecSendSocketAwaitables.Push(socketAwaitable);
            //create an object that we can write data to.
            //receiveSendToken.CreateNewDataHolder();
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
