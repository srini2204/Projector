using Projector.IO.Protocol.Responses;
using System;
using System.Text;

namespace Projector.IO.Implementation.Protocol
{
    class SchemaEvent : IResponse
    {
        private byte[] _data;

        public SchemaEvent()
        {
            //convert the message to byte array
            var arrayOfBytesInMessage = Encoding.ASCII.GetBytes("s");

            //So, now we convert the length integer into a byte array
            var arrayOfBytesInPrefix = BitConverter.GetBytes(arrayOfBytesInMessage.Length);

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
