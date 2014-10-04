using System;
using System.Text;

namespace Projector.IO.Protocol.Responses
{
    public class OkResponse : IResponse
    {
        private byte[] _data;

        public OkResponse()
        {
            //convert the message to byte array
            byte[] arrayOfBytesInMessage = Encoding.ASCII.GetBytes("o");

            //So, now we convert the length integer into a byte array
            byte[] arrayOfBytesInPrefix = BitConverter.GetBytes(arrayOfBytesInMessage.Length);

            //Create the byte array to send.
            var resultArray = new byte[arrayOfBytesInPrefix.Length + arrayOfBytesInMessage.Length];

            Buffer.BlockCopy(arrayOfBytesInPrefix, 0, resultArray, 0, arrayOfBytesInPrefix.Length);
            Buffer.BlockCopy(arrayOfBytesInMessage, 0, resultArray, arrayOfBytesInPrefix.Length, arrayOfBytesInMessage.Length);

            _data = resultArray;
        }
        public byte[] GetBytes()
        {
            return _data;
        }
    }
}
