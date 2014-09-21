using System;
using System.Net;

namespace SocketAsyncServer
{
    public class SocketListenerSettings
    {
        public SocketListenerSettings(int maxConnections,
                                        int excessSaeaObjectsInPool,
                                        int backlog,
                                        int maxSimultaneousAcceptOps,
                                        int receivePrefixLength,
                                        int receiveBufferSize,
                                        int sendPrefixLength,
                                        int opsToPreAlloc, 
                                        IPEndPoint theLocalEndPoint)
        {
            MaxConnections = maxConnections;
            NumberOfSaeaForRecSend = MaxConnections + excessSaeaObjectsInPool;
            Backlog = backlog;
            MaxAcceptOps = maxSimultaneousAcceptOps;
            ReceivePrefixLength = receivePrefixLength;
            BufferSize = receiveBufferSize;
            SendPrefixLength = sendPrefixLength;
            OpsToPreAllocate = opsToPreAlloc;
            LocalEndPoint = theLocalEndPoint;
        }

        /// <summary>
        /// The maximum number of connections the sample is designed to handle simultaneously 
        /// </summary>
        public Int32 MaxConnections { get; private set; }

        /// <summary>
        /// This variable allows us to create some extra SAEA objects for the pool, if we wish.
        /// </summary>
        public Int32 NumberOfSaeaForRecSend { get; private set; }

        /// <summary>
        /// Max # of pending connections the listener can hold in queue
        /// </summary>
        public Int32 Backlog { get; private set; }

        /// <summary>
        /// Tells us how many objects to put in pool for accept operations
        /// </summary>
        public Int32 MaxAcceptOps { get; private set; }

        /// <summary>
        /// Length of message prefix for receive ops
        /// </summary>
        public Int32 ReceivePrefixLength { get; private set; }

        /// <summary>
        /// Buffer size to use for each socket receive operation
        /// </summary>
        public Int32 BufferSize { get; private set; }

        /// <summary>
        /// Length of message prefix for send ops
        /// </summary>
        public Int32 SendPrefixLength { get; private set; }

        /// <summary>
        /// See comments in buffer manager
        /// </summary>
        public Int32 OpsToPreAllocate { get; private set; }

        /// <summary>
        /// Endpoint for the listener
        /// </summary>
        public IPEndPoint LocalEndPoint { get; private set; }
    }
}
