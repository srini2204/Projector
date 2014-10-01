using Projector.IO.SocketHelpers;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Client
{
    public sealed class SocketClient
    {

        private readonly SocketClientSettings _socketClientSettings;

        private readonly PrefixHandler _prefixHandler;
        private readonly MessageHandler _messageHandler;

        private SocketAsyncEventArgs _receiveSendEventArgs;
        private SocketAwaitable _socketAwaitable;




        public SocketClient(SocketClientSettings theSocketClientSettings)
        {
            _socketClientSettings = theSocketClientSettings;
            _prefixHandler = new PrefixHandler();
            _messageHandler = new MessageHandler();
        }

        public async Task ConnectAsync()
        {
            var socketAsyncEventArgs = new SocketAsyncEventArgs();


            socketAsyncEventArgs.RemoteEndPoint = _socketClientSettings.ServerEndPoint;

            socketAsyncEventArgs.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connectSocketAwaitable = new SocketAwaitable(socketAsyncEventArgs);
            await socketAsyncEventArgs.AcceptSocket.ConnectAsync(connectSocketAwaitable);

            if (socketAsyncEventArgs.SocketError == SocketError.Success)
            {
                _receiveSendEventArgs = new SocketAsyncEventArgs();
                _socketAwaitable = new SocketAwaitable(_receiveSendEventArgs);

                _receiveSendEventArgs.SetBuffer(new byte[_socketClientSettings.BufferSize], 0, _socketClientSettings.BufferSize);

                var receiveSendToken = new DataHoldingUserToken(_receiveSendEventArgs.Offset, _socketClientSettings.PrefixLength);
                receiveSendToken.CreateNewDataHolder();
                _receiveSendEventArgs.UserToken = receiveSendToken;
                _receiveSendEventArgs.AcceptSocket = socketAsyncEventArgs.AcceptSocket;

            }
            else if ((socketAsyncEventArgs.SocketError != SocketError.ConnectionRefused)
                && (socketAsyncEventArgs.SocketError != SocketError.TimedOut)
                && (socketAsyncEventArgs.SocketError != SocketError.HostUnreachable))
            {
                CloseSocket(socketAsyncEventArgs.AcceptSocket);
            }
        }

        public async Task SendAsync(byte[] data)
        {
            DataHoldingUserToken receiveSendToken = (DataHoldingUserToken)_receiveSendEventArgs.UserToken;
            receiveSendToken.sendBytesRemainingCount = data.Length;
            do
            {

                if (receiveSendToken.sendBytesRemainingCount <= _socketClientSettings.BufferSize)
                {
                    _receiveSendEventArgs.SetBuffer(receiveSendToken.bufferOffset, receiveSendToken.sendBytesRemainingCount);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(data, receiveSendToken.bytesSentAlreadyCount, _receiveSendEventArgs.Buffer, receiveSendToken.bufferOffset, receiveSendToken.sendBytesRemainingCount);
                }
                else
                {
                    //We cannot try to set the buffer any larger than its size.
                    //So since receiveSendToken.sendBytesRemaining > its size, we just
                    //set it to the maximum size, to send the most data possible.
                    _receiveSendEventArgs.SetBuffer(receiveSendToken.bufferOffset, _socketClientSettings.BufferSize);
                    //Copy the bytes to the buffer associated with this SAEA object.
                    Buffer.BlockCopy(data, receiveSendToken.bytesSentAlreadyCount, _receiveSendEventArgs.Buffer, receiveSendToken.bufferOffset, _socketClientSettings.BufferSize);

                    //We'll change the value of sendUserToken.sendBytesRemaining
                    //in the ProcessSend method.
                }

                await _receiveSendEventArgs.AcceptSocket.SendAsync(_socketAwaitable);



                if (_receiveSendEventArgs.SocketError == SocketError.Success)
                {
                    receiveSendToken.sendBytesRemainingCount = receiveSendToken.sendBytesRemainingCount - _receiveSendEventArgs.BytesTransferred;


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
        }


        public async Task<byte[]> ReceiveAsync()
        {
            bool incomingTcpMessageIsReady = false;
            DataHoldingUserToken receiveSendToken = (DataHoldingUserToken)_receiveSendEventArgs.UserToken;

            do
            {
                //Set buffer for receive.
                _receiveSendEventArgs.SetBuffer(receiveSendToken.bufferOffset, _socketClientSettings.BufferSize);

                await _receiveSendEventArgs.AcceptSocket.ReceiveAsync(_socketAwaitable);

                // If there was a socket error, close the connection.
                if (_receiveSendEventArgs.SocketError != SocketError.Success)
                {
                    receiveSendToken.Reset();
                    await DisconnectAsync();
                    return new byte[] { }; // exception?
                }

                //If no data was received, close the connection.
                if (_receiveSendEventArgs.BytesTransferred == 0)
                {
                    receiveSendToken.Reset();
                    await DisconnectAsync();
                    return new byte[] { }; // exception?
                }

                var remainingBytesToProcess = _receiveSendEventArgs.BytesTransferred;


                // If we have not got all of the prefix then we need to work on it. 
                // receivedPrefixBytesDoneCount tells us how many prefix bytes were
                // processed during previous receive ops which contained data for 
                // this message. (In normal use, usually there will NOT have been any 
                // previous receive ops here. So receivedPrefixBytesDoneCount would be 0.)
                if (receiveSendToken.receivedPrefixBytesDoneCount < _socketClientSettings.PrefixLength)
                {
                    remainingBytesToProcess = _prefixHandler.HandlePrefix(_receiveSendEventArgs, receiveSendToken, remainingBytesToProcess);

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
                incomingTcpMessageIsReady = _messageHandler.HandleMessage(_receiveSendEventArgs, receiveSendToken, remainingBytesToProcess);

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

            return resultBytes;

        }

        public async Task DisconnectAsync()
        {
            var receiveSendToken = (DataHoldingUserToken)_receiveSendEventArgs.UserToken;


            _receiveSendEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);

            await _receiveSendEventArgs.AcceptSocket.DisconnectAsync(_socketAwaitable);

            if (_receiveSendEventArgs.SocketError != SocketError.Success)
            {

            }

            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            _receiveSendEventArgs.AcceptSocket.Close();


            //create an object that we can write data to.
            receiveSendToken.CreateNewDataHolder();
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