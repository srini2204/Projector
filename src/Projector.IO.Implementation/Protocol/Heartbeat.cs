using Projector.IO.Protocol.Responses;
using System;

namespace Projector.IO.Implementation.Protocol
{
    class Heartbeat : IResponse
    {
        private readonly byte[] _data;

        public Heartbeat()
        {
            //So, now we convert the length integer into a byte array
            var arrayOfBytesInPrefix = BitConverter.GetBytes(0);

            //Create the byte array to send.
            var resultArray = new byte[arrayOfBytesInPrefix.Length];

            Buffer.BlockCopy(arrayOfBytesInPrefix, 0, resultArray, 0, arrayOfBytesInPrefix.Length);

            _data = resultArray;
        }

        public byte[] GetBytes()
        {
            return _data;
        }
    }
}
