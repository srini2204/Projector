using System;
using System.Threading;

namespace Projector.IO.SocketHelpers
{
    class DataHoldingUserToken
    {
        internal DataHolder theDataHolder;

        internal readonly int bufferOffset;
        internal readonly int prefixLength;
        internal readonly int permanentReceiveMessageOffset;

        internal int lengthOfCurrentIncomingMessage;

        //receiveMessageOffset is used to mark the byte position where the message
        //begins in the receive buffer. This value can sometimes be out of
        //bounds for the data stream just received. But, if it is out of bounds, the 
        //code will not access it.
        internal int receiveMessageOffset;
        internal Byte[] byteArrayForPrefix;
        internal int receivedPrefixBytesDoneCount = 0;
        internal int receivedMessageBytesDoneCount = 0;
        //This variable will be needed to calculate the value of the
        //receiveMessageOffset variable in one situation. Notice that the
        //name is similar but the usage is different from the variable
        //receiveSendToken.receivePrefixBytesDone.
        internal int recPrefixBytesDoneThisOp = 0;

        internal int sendBytesRemainingCount;
        internal Byte[] dataToSend;
        internal int bytesSentAlreadyCount;

        private int _mainSessionId = 1000000;

        //The session ID correlates with all the data sent in a connected session.
        //It is different from the transmission ID in the DataHolder, which relates
        //to one TCP message. A connected session could have many messages, if you
        //set up your app to allow it.
        private int sessionId;

        public DataHoldingUserToken(int offset, int prefixLength)
        {

            this.bufferOffset = offset;
            this.prefixLength = prefixLength;
            this.receiveMessageOffset = offset + prefixLength;
            this.permanentReceiveMessageOffset = this.receiveMessageOffset;
        }

        internal void CreateNewDataHolder()
        {
            theDataHolder = new DataHolder();
        }

        //Used to create sessionId variable in DataHoldingUserToken.
        //Called in ProcessAccept().
        internal void CreateSessionId()
        {
            sessionId = Interlocked.Increment(ref _mainSessionId);
        }

        public int SessionId
        {
            get
            {
                return this.sessionId;
            }
        }

        public void Reset()
        {
            this.receivedPrefixBytesDoneCount = 0;
            this.receivedMessageBytesDoneCount = 0;
            this.recPrefixBytesDoneThisOp = 0;
            this.receiveMessageOffset = this.permanentReceiveMessageOffset;
        }
    }
}
