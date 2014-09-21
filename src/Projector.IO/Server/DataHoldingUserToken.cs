using System;
using System.Net.Sockets;
using System.Threading;

namespace Projector.IO.Server
{
    class DataHoldingUserToken
    {
        internal Mediator theMediator;
        internal DataHolder theDataHolder;

        internal int socketHandleNumber;

        internal readonly int bufferOffsetReceive;
        internal readonly int permanentReceiveMessageOffset;
        internal readonly int bufferOffsetSend;

        private int idOfThisObject; //for testing only        

        internal int lengthOfCurrentIncomingMessage;

        //receiveMessageOffset is used to mark the byte position where the message
        //begins in the receive buffer. This value can sometimes be out of
        //bounds for the data stream just received. But, if it is out of bounds, the 
        //code will not access it.
        internal int receiveMessageOffset;
        internal Byte[] byteArrayForPrefix;
        internal readonly int receivePrefixLength;
        internal int receivedPrefixBytesDoneCount = 0;
        internal int receivedMessageBytesDoneCount = 0;
        //This variable will be needed to calculate the value of the
        //receiveMessageOffset variable in one situation. Notice that the
        //name is similar but the usage is different from the variable
        //receiveSendToken.receivePrefixBytesDone.
        internal int recPrefixBytesDoneThisOp = 0;

        internal int sendBytesRemainingCount;
        internal readonly int sendPrefixLength;
        internal Byte[] dataToSend;
        internal int bytesSentAlreadyCount;

        private int _mainSessionId = 1000000;

        //The session ID correlates with all the data sent in a connected session.
        //It is different from the transmission ID in the DataHolder, which relates
        //to one TCP message. A connected session could have many messages, if you
        //set up your app to allow it.
        private int sessionId;

        public DataHoldingUserToken(SocketAsyncEventArgs e, int rOffset, int sOffset, int receivePrefixLength, int sendPrefixLength, int identifier)
        {
            this.idOfThisObject = identifier;

            //Create a Mediator that has a reference to the SAEA object.
            this.theMediator = new Mediator(e);
            this.bufferOffsetReceive = rOffset;
            this.bufferOffsetSend = sOffset;
            this.receivePrefixLength = receivePrefixLength;
            this.sendPrefixLength = sendPrefixLength;
            this.receiveMessageOffset = rOffset + receivePrefixLength;
            this.permanentReceiveMessageOffset = this.receiveMessageOffset;
        }

        //Let's use an ID for this object during testing, just so we can see what
        //is happening better if we want to.
        public int TokenId
        {
            get
            {
                return this.idOfThisObject;
            }
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
