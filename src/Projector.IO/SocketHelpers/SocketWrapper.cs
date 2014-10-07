using System;
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
            _prefixLength = prefixLength;
        }

        public async Task<bool> SendAsync(byte[] data)
        {
            var sendSocketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

            sendSocketAwaitable.EventArgs.AcceptSocket = _socket;

            var sendEventArgs = sendSocketAwaitable.EventArgs;

            var receiveSendToken = (DataHoldingUserToken)sendEventArgs.UserToken;
            var sendBytesRemainingCount = data.Length;
            var bytesSentAlreadyCount = 0;

            do
            {

                if (sendBytesRemainingCount <= _bufferSize)
                {
                    sendEventArgs.SetBuffer(receiveSendToken.bufferOffset, sendBytesRemainingCount);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(data, bytesSentAlreadyCount, sendEventArgs.Buffer, receiveSendToken.bufferOffset, sendBytesRemainingCount);
                }
                else
                {
                    //We cannot try to set the buffer any larger than its size.
                    //So since receiveSendToken.sendBytesRemaining > its size, we just
                    //set it to the maximum size, to send the most data possible.
                    sendEventArgs.SetBuffer(receiveSendToken.bufferOffset, _bufferSize);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(data, bytesSentAlreadyCount, sendEventArgs.Buffer, receiveSendToken.bufferOffset, _bufferSize);
                }

                await sendEventArgs.AcceptSocket.SendAsync(sendSocketAwaitable);



                if (sendEventArgs.SocketError == SocketError.Success)
                {
                    sendBytesRemainingCount = sendBytesRemainingCount - sendEventArgs.BytesTransferred;
                    bytesSentAlreadyCount = sendEventArgs.BytesTransferred;
                }
                else
                {
                    // We'll just close the socket if there was a
                    // socket error when receiving data from the client.
                    receiveSendToken.Reset();
                    await DisconnectAsync();
                    return false;
                }
            }
            while (sendBytesRemainingCount != 0);

            sendSocketAwaitable.EventArgs.AcceptSocket = null;
            _poolOfRecSendSocketAwaitables.Push(sendSocketAwaitable);

            return true;
        }


        public async Task<byte[]> ReceiveAsync()
        {
            var socketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

            socketAwaitable.EventArgs.AcceptSocket = _socket;

            var eventArgs = socketAwaitable.EventArgs;

            var incomingTcpMessageIsReady = false;
            var receiveSendToken = (DataHoldingUserToken)eventArgs.UserToken;

            do
            {
                //Set buffer for receive.
                eventArgs.SetBuffer(receiveSendToken.bufferOffset, _bufferSize);

                await eventArgs.AcceptSocket.ReceiveAsync(socketAwaitable);

                // If there was a socket error, close the connection.
                if (eventArgs.SocketError != SocketError.Success || eventArgs.BytesTransferred == 0)
                {
                    receiveSendToken.Reset();
                    await DisconnectAsync();
                    return null;
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

                    if (remainingBytesToProcess == 0 && receiveSendToken.receivedPrefixBytesDoneCount < _prefixLength)
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

        public async Task DisconnectAsync()
        {
            var socketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

            socketAwaitable.EventArgs.AcceptSocket = _socket;

            try
            {
                _socket.Shutdown(SocketShutdown.Both);

                await _socket.DisconnectAsync(socketAwaitable);

                if (socketAwaitable.EventArgs.SocketError != SocketError.Success)
                {

                }

                //This method closes the socket and releases all resources, both
                //managed and unmanaged. It internally calls Dispose.
                _socket.Close();
            }
            catch (ObjectDisposedException)
            {
                //expected. this is the wait to cancel async IO :-/
            }


            _poolOfRecSendSocketAwaitables.Push(socketAwaitable);
            //create an object that we can write data to.
            //receiveSendToken.CreateNewDataHolder();
        }
    }
}
