using System.Net;

namespace Projector.IO.Client
{
    public class SocketClientSettings
    {
        public SocketClientSettings(IPEndPoint theServerEndPoint, int prefixLength, int bufferSize, int opsToPreAlloc)
        {
            PrefixLength = prefixLength;
            BufferSize = bufferSize;
            OpsToPreAllocate = opsToPreAlloc;
            ServerEndPoint = theServerEndPoint;
        }

        public int PrefixLength { get; private set; }
        public int BufferSize { get; private set; }
        public int OpsToPreAllocate { get; private set; }

        public IPEndPoint ServerEndPoint { get; private set; }


    }
}
