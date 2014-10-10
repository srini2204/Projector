using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.SocketHelpers
{
    public class SocketWrapper
    {
        private readonly ObjectPool<SocketAwaitable> _poolOfRecSendSocketAwaitables;
        private readonly ISocket _socket;
        private readonly int _bufferSize;
        private readonly int _prefixLength;

        public SocketWrapper(ObjectPool<SocketAwaitable> poolOfRecSendSocketAwaitables, ISocket socket, int prefixLength, int bufferSize)
        {
            _poolOfRecSendSocketAwaitables = poolOfRecSendSocketAwaitables;
            _socket = socket;
            _bufferSize = bufferSize;
            _prefixLength = prefixLength;
        }

        public async Task<bool> SendAsync(Stream stream)
        {
            SocketAwaitable sendSocketAwaitable = null;
            try
            {

                sendSocketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

                var sendEventArgs = sendSocketAwaitable.EventArgs;

                var receiveSendToken = (DataHoldingUserToken)sendEventArgs.UserToken;
                var sendBytesRemainingCount = stream.Position;
                stream.Position = 0;

                do
                {

                    if (sendBytesRemainingCount <= _bufferSize)
                    {
                        sendEventArgs.SetBuffer(receiveSendToken.BufferOffset, (int)sendBytesRemainingCount);

                        await stream.ReadAsync(sendEventArgs.Buffer, receiveSendToken.BufferOffset, (int)sendBytesRemainingCount);

                    }
                    else
                    {
                        //We cannot try to set the buffer any larger than its size.
                        //So since receiveSendToken.sendBytesRemaining > its size, we just
                        //set it to the maximum size, to send the most data possible.
                        sendEventArgs.SetBuffer(receiveSendToken.BufferOffset, _bufferSize);

                        await stream.ReadAsync(sendEventArgs.Buffer, receiveSendToken.BufferOffset, _bufferSize);
                    }

                    await _socket.SendAsync(sendSocketAwaitable);



                    if (sendEventArgs.SocketError == SocketError.Success)
                    {
                        // check if the whole buffer was sent
                        var bytesRemained = sendEventArgs.Count - sendSocketAwaitable.BytesTransferred;
                        if (bytesRemained > 0)
                        {
                            stream.Position -= bytesRemained;
                        }

                        sendBytesRemainingCount = sendBytesRemainingCount - sendSocketAwaitable.BytesTransferred;
                    }
                    else
                    {
                        // We'll just close the socket if there was a
                        // socket error when receiving data from the client.
                        await DisconnectAsync();
                        return false;
                    }
                }
                while (sendBytesRemainingCount != 0);
            }
            finally
            {
                if (sendSocketAwaitable != null)
                {
                    _poolOfRecSendSocketAwaitables.Push(sendSocketAwaitable);
                }
            }

            return true;
        }


        public async Task<bool> ReceiveAsync(Stream stream)
        {
            SocketAwaitable socketAwaitable = null;
            try
            {

                socketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

                var eventArgs = socketAwaitable.EventArgs;

                var receiveSendToken = (DataHoldingUserToken)eventArgs.UserToken;

                //Set buffer for receive.
                eventArgs.SetBuffer(receiveSendToken.BufferOffset, _bufferSize);

                var lengthOfCurrentIncomingMessage = -1;

                do
                {
                    await _socket.ReceiveAsync(socketAwaitable);

                    // If there was a socket error, close the connection.
                    if (eventArgs.SocketError != SocketError.Success || socketAwaitable.BytesTransferred == 0)
                    {
                        await DisconnectAsync();
                        return false;
                    }

                    await stream.WriteAsync(eventArgs.Buffer, receiveSendToken.BufferOffset, socketAwaitable.BytesTransferred);

                    if (lengthOfCurrentIncomingMessage == -1 && stream.Position >= _prefixLength)
                    {
                        lengthOfCurrentIncomingMessage = BitConverter.ToInt32(eventArgs.Buffer, 0);
                    }
                }
                while (lengthOfCurrentIncomingMessage == -1 || stream.Position < lengthOfCurrentIncomingMessage + _prefixLength);

                return true;

            }
            finally
            {
                if (socketAwaitable != null)
                {
                    _poolOfRecSendSocketAwaitables.Push(socketAwaitable);
                }
            }
        }

        public async Task DisconnectAsync()
        {
            var socketAwaitable = _poolOfRecSendSocketAwaitables.Pop();

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
