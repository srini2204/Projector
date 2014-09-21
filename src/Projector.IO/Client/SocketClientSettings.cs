using System.Net;

namespace SocketClientAsyncTester
{
    public class SocketClientSettings
    {
        public SocketClientSettings(IPEndPoint theServerEndPoint, int receivePrefixLength, int bufferSize, int sendPrefixLength, int opsToPreAlloc)
        {
            ReceivePrefixLength = receivePrefixLength;
            BufferSize = bufferSize;
            SendPrefixLength = sendPrefixLength;
            OpsToPreAllocate = opsToPreAlloc;
            ServerEndPoint = theServerEndPoint;
        }

        public int ReceivePrefixLength { get; private set; }
        public int BufferSize { get; private set; }
        public int SendPrefixLength { get; private set; }
        public int OpsToPreAllocate { get; private set; }

        public IPEndPoint ServerEndPoint { get; private set; }


    }
}
