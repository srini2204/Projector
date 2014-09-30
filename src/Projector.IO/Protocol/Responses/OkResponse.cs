using System;
using System.Text;

namespace Projector.IO.Protocol.Responses
{
    public class OkResponse : IResponse
    {
        public byte[] GetBytes()
        {
            //convert the message to byte array
            byte[] arrayOfBytesInMessage = Encoding.ASCII.GetBytes("o");

            //So, now we convert the length integer into a byte array.
            //Aren't byte arrays wonderful? Maybe you'll dream about byte arrays tonight!
            byte[] arrayOfBytesInPrefix = BitConverter.GetBytes(arrayOfBytesInMessage.Length);

            //Create the byte array to send.
            var resultArray = new byte[arrayOfBytesInPrefix.Length + arrayOfBytesInMessage.Length];

            Buffer.BlockCopy(arrayOfBytesInPrefix, 0, resultArray, 0, arrayOfBytesInPrefix.Length);
            Buffer.BlockCopy(arrayOfBytesInMessage, 0, resultArray, arrayOfBytesInPrefix.Length, arrayOfBytesInMessage.Length);

            return resultArray;
        }
    }
}
