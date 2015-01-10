
namespace Projector.IO.SocketHelpers
{
    public class DataHoldingUserToken
    {
        public DataHoldingUserToken(int offset)
        {
            BufferOffset = offset;
        }

        public int BufferOffset { get; private set; }
    }
}
