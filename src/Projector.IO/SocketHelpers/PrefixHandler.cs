using System;
using System.Net.Sockets;

namespace Projector.IO.SocketHelpers
{
    class PrefixHandler
    {
        public int HandlePrefix(SocketAsyncEventArgs e, DataHoldingUserToken receiveSendToken, int remainingBytesToProcess)
        {
            //receivedPrefixBytesDoneCount tells us how many prefix bytes were
            //processed during previous receive ops which contained data for 
            //this message. Usually there will NOT have been any previous 
            //receive ops here. So in that case,
            //receiveSendToken.receivedPrefixBytesDoneCount would equal 0.
            //Create a byte array to put the new prefix in, if we have not
            //already done it in a previous loop.
            if (receiveSendToken.receivedPrefixBytesDoneCount == 0)
            {
                receiveSendToken.byteArrayForPrefix = new Byte[receiveSendToken.prefixLength];
            }

            // If this next if-statement is true, then we have received >=
            // enough bytes to have the prefix. So we can determine the 
            // length of the message that we are working on.
            if (remainingBytesToProcess >= receiveSendToken.prefixLength - receiveSendToken.receivedPrefixBytesDoneCount)
            {
                //Now copy that many bytes to byteArrayForPrefix.
                //We can use the variable receiveMessageOffset as our main
                //index to show which index to get data from in the TCP
                //buffer.
                Buffer.BlockCopy(e.Buffer, receiveSendToken.receiveMessageOffset - receiveSendToken.prefixLength + receiveSendToken.receivedPrefixBytesDoneCount, receiveSendToken.byteArrayForPrefix, receiveSendToken.receivedPrefixBytesDoneCount, receiveSendToken.prefixLength - receiveSendToken.receivedPrefixBytesDoneCount);

                remainingBytesToProcess = remainingBytesToProcess - receiveSendToken.prefixLength + receiveSendToken.receivedPrefixBytesDoneCount;

                receiveSendToken.recPrefixBytesDoneThisOp = receiveSendToken.prefixLength - receiveSendToken.receivedPrefixBytesDoneCount;

                receiveSendToken.receivedPrefixBytesDoneCount = receiveSendToken.prefixLength;

                receiveSendToken.lengthOfCurrentIncomingMessage = BitConverter.ToInt32(receiveSendToken.byteArrayForPrefix, 0);


            }

            //This next else-statement deals with the situation 
            //where we have some bytes
            //of this prefix in this receive operation, but not all.
            else
            {
                //Write the bytes to the array where we are putting the
                //prefix data, to save for the next loop.
                Buffer.BlockCopy(e.Buffer, receiveSendToken.receiveMessageOffset - receiveSendToken.prefixLength + receiveSendToken.receivedPrefixBytesDoneCount, receiveSendToken.byteArrayForPrefix, receiveSendToken.receivedPrefixBytesDoneCount, remainingBytesToProcess);

                receiveSendToken.recPrefixBytesDoneThisOp = remainingBytesToProcess;
                receiveSendToken.receivedPrefixBytesDoneCount += remainingBytesToProcess;
                remainingBytesToProcess = 0;
            }

            // This section is needed when we have received
            // an amount of data exactly equal to the amount needed for the prefix,
            // but no more. And also needed with the situation where we have received
            // less than the amount of data needed for prefix. 
            if (remainingBytesToProcess == 0)
            {
                receiveSendToken.receiveMessageOffset = receiveSendToken.receiveMessageOffset - receiveSendToken.recPrefixBytesDoneThisOp;
                receiveSendToken.recPrefixBytesDoneThisOp = 0;
            }
            return remainingBytesToProcess;
        }
    }
}
