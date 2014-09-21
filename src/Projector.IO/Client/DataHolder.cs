using System;
using System.Collections.Generic;

namespace Projector.IO.Client
{
    public class DataHolder
    {
        private int numberOfMessagesSent = 0;

        //We'll just send a string message. And have one or more messages, so
        //we need an array.
        internal string[] arrayOfMessagesToSend;

        internal Byte[] dataMessageReceived;

        //Since we are creating a List<T> of message data, we'll
        //need to decode it later, if we want to read a string.
        internal List<byte[]> listOfMessagesReceived = new List<byte[]>();

        public DataHolder()
        {
        }

        public int NumberOfMessagesSent
        {
            get
            {
                return this.numberOfMessagesSent;
            }
            set
            {
                this.numberOfMessagesSent = value;
            }
        }

        //write the array of messages to send
        internal void PutMessagesToSend(string[] theArrayOfMessagesToSend)
        {
            this.arrayOfMessagesToSend = theArrayOfMessagesToSend;
        }
    }
}
