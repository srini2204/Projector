using System.Net;

namespace Projector.IO.Server
{
    public class SocketListenerSettings
    {
        public SocketListenerSettings(int maxConnections,
                                        int excessSaeaObjectsInPool,
                                        int backlog,
                                        int prefixLength,
                                        int bufferSize,
                                        int opsToPreAlloc,
                                        IPEndPoint theLocalEndPoint)
        {
            MaxConnections = maxConnections;
            NumberOfSaeaForRecSend = MaxConnections + excessSaeaObjectsInPool;
            Backlog = backlog;
            PrefixLength = prefixLength;
            BufferSize = bufferSize;
            OpsToPreAllocate = opsToPreAlloc;
            LocalEndPoint = theLocalEndPoint;
        }

        /// <summary>
        /// The maximum number of connections the sample is designed to handle simultaneously 
        /// </summary>
        public int MaxConnections { get; private set; }

        /// <summary>
        /// This variable allows us to create some extra SAEA objects for the pool, if we wish.
        /// </summary>
        public int NumberOfSaeaForRecSend { get; private set; }

        /// <summary>
        /// Max # of pending connections the listener can hold in queue
        /// </summary>
        public int Backlog { get; private set; }

        /// <summary>
        /// Length of message prefix for receive ops
        /// </summary>
        public int PrefixLength { get; private set; }

        /// <summary>
        /// Buffer size to use for each socket operation
        /// </summary>
        public int BufferSize { get; private set; }

        /// <summary>
        /// See comments in buffer manager
        /// </summary>
        public int OpsToPreAllocate { get; private set; }

        /// <summary>
        /// Endpoint for the listener
        /// </summary>
        public IPEndPoint LocalEndPoint { get; private set; }
    }
}
