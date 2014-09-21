using System;
using System.Net.Sockets;

namespace Projector.IO.Client
{
    class PrefixHandler
    {
        public Int32 HandlePrefix(SocketAsyncEventArgs e, DataHoldingUserToken receiveSendToken, Int32 remainingBytesToProcess)
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

                receiveSendToken.byteArrayForPrefix = new Byte[receiveSendToken.receivePrefixLength];
            }

            //If this next if-statement is true, then we have received at 
            //least enough bytes to have the prefix. So we can determine the 
            //length of the message that we are working on.
            if (remainingBytesToProcess >= receiveSendToken.receivePrefixLength - receiveSendToken.receivedPrefixBytesDoneCount)
            {

                //Now copy that many bytes to byteArrayForPrefix.
                //We can use the variable receiveMessageOffset as our main
                //index to show which index to get data from in the TCP
                //buffer.
                Buffer.BlockCopy(e.Buffer, receiveSendToken.receiveMessageOffset - receiveSendToken.receivePrefixLength + receiveSendToken.receivedPrefixBytesDoneCount, receiveSendToken.byteArrayForPrefix, receiveSendToken.receivedPrefixBytesDoneCount, receiveSendToken.receivePrefixLength - receiveSendToken.receivedPrefixBytesDoneCount);

                remainingBytesToProcess = remainingBytesToProcess - receiveSendToken.receivePrefixLength + receiveSendToken.receivedPrefixBytesDoneCount;

                receiveSendToken.recPrefixBytesDoneThisOp = receiveSendToken.receivePrefixLength - receiveSendToken.receivedPrefixBytesDoneCount;

                receiveSendToken.receivedPrefixBytesDoneCount = receiveSendToken.receivePrefixLength;

                receiveSendToken.lengthOfCurrentIncomingMessage = BitConverter.ToInt32(receiveSendToken.byteArrayForPrefix, 0);




                return remainingBytesToProcess;
            }

            //This next else-statement deals with the situation 
            //where we have some bytes
            //of this prefix in this receive operation, but not all.
            else
            {

                //Write the bytes to the array where we are putting the
                //prefix data, to save for the next loop.
                Buffer.BlockCopy(e.Buffer, receiveSendToken.receiveMessageOffset - receiveSendToken.receivePrefixLength + receiveSendToken.receivedPrefixBytesDoneCount, receiveSendToken.byteArrayForPrefix, receiveSendToken.receivedPrefixBytesDoneCount, remainingBytesToProcess);

                receiveSendToken.recPrefixBytesDoneThisOp = remainingBytesToProcess;
                receiveSendToken.receivedPrefixBytesDoneCount += remainingBytesToProcess;
                remainingBytesToProcess = 0;
            }

            // Deal with the situation where we got exactly the amount of data
            // needed for the prefix, but no more.
            if (remainingBytesToProcess == 0)
            {
                receiveSendToken.receiveMessageOffset = receiveSendToken.receiveMessageOffset - receiveSendToken.recPrefixBytesDoneThisOp;
                receiveSendToken.recPrefixBytesDoneThisOp = 0;
            }
            return remainingBytesToProcess;
        }
    }
}
