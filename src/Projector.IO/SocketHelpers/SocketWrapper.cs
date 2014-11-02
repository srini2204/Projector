using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.SocketHelpers
{
    public class SocketWrapper
    {
        private readonly SocketAwaitable _sendSocketAwaitable;
        private readonly SocketAwaitable _receiveSocketAwaitable;
        private readonly ISocket _socket;
        private readonly int _bufferSize;

        public SocketWrapper(SocketAwaitable sendSocketAwaitable, SocketAwaitable receiveSocketAwaitable, ISocket socket, int bufferSize)
        {
            _sendSocketAwaitable = sendSocketAwaitable;
            _receiveSocketAwaitable = receiveSocketAwaitable;
            _socket = socket;
            _bufferSize = bufferSize;
        }

        public async Task<bool> SendAsync(Stream stream)
        {
            if (stream.Position == 0)
            {
                throw new ArgumentException("Stream position is 0. There is nothing to send");
            }

            var sendEventArgs = _sendSocketAwaitable.EventArgs;

            var receiveSendToken = (DataHoldingUserToken)sendEventArgs.UserToken;
            var sendBytesRemainingCount = stream.Length;

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

                await _socket.SendAsync(_sendSocketAwaitable);



                if (sendEventArgs.SocketError == SocketError.Success)
                {
                    // check if the whole buffer was sent
                    var bytesRemained = sendEventArgs.Count - _sendSocketAwaitable.BytesTransferred;
                    if (bytesRemained > 0)
                    {
                        stream.Position -= bytesRemained;
                    }

                    sendBytesRemainingCount = sendBytesRemainingCount - _sendSocketAwaitable.BytesTransferred;
                }
                else
                {
                    return false;
                }
            }
            while (sendBytesRemainingCount != 0);


            return true;
        }


        public async Task<bool> ReceiveAsync(Stream stream)
        {
            var eventArgs = _receiveSocketAwaitable.EventArgs;

            var receiveSendToken = (DataHoldingUserToken)eventArgs.UserToken;

            //Set buffer for receive.
            eventArgs.SetBuffer(receiveSendToken.BufferOffset, _bufferSize);


            await _socket.ReceiveAsync(_receiveSocketAwaitable);

            // If there was a socket error, close the connection.
            if (eventArgs.SocketError != SocketError.Success || _receiveSocketAwaitable.BytesTransferred == 0)
            {
                return false;
            }

            await stream.WriteAsync(eventArgs.Buffer, receiveSendToken.BufferOffset, _receiveSocketAwaitable.BytesTransferred);

            return true;

        }

        public async Task DisconnectAsync()
        {
            var socketAwaitable = _sendSocketAwaitable;

            _socket.Shutdown(SocketShutdown.Both);

            await _socket.DisconnectAsync(socketAwaitable);

            if (socketAwaitable.EventArgs.SocketError != SocketError.Success)
            {

            }

            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            _socket.Close();

        }
    }
}
