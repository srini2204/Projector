using System;

namespace Projector.IO.Client
{
    class DataHoldingUserToken
    {
        internal DataHolder theDataHolder;

        private int idOfThisObject; //for testing only

        internal readonly int sendPrefixLength;
        internal readonly int receivePrefixLength;
        internal int receivedPrefixBytesDoneCount = 0;
        internal int receivedMessageBytesDoneCount = 0;
        internal Byte[] byteArrayForPrefix;
        internal int receiveMessageOffset;
        internal int recPrefixBytesDoneThisOp = 0;
        internal int lengthOfCurrentIncomingMessage;
        internal readonly int bufferOffsetReceive;
        internal readonly int permanentReceiveMessageOffset;
        internal readonly int bufferOffsetSend;
        internal Byte[] dataToSend;
        internal int sendBytesRemaining;
        internal int bytesSentAlready;

        public DataHoldingUserToken(int rOffset, int sOffset, int receivePrefixLength, int sendPrefixLength, int identifier)
        {
            this.idOfThisObject = identifier;
            this.bufferOffsetReceive = rOffset;
            this.bufferOffsetSend = sOffset;
            this.receivePrefixLength = receivePrefixLength;
            this.sendPrefixLength = sendPrefixLength;
            this.receiveMessageOffset = rOffset + receivePrefixLength;
            this.permanentReceiveMessageOffset = this.receiveMessageOffset;
        }

        public int TokenId
        {
            get
            {
                return idOfThisObject;
            }
        }

        internal void CreateNewDataHolder()
        {
            theDataHolder = new DataHolder();
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
