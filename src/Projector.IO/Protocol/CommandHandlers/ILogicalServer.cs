using Projector.IO.SocketHelpers;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Protocol.CommandHandlers
{
    public interface ILogicalServer
    {
        Task RegisterConnectedClient(IPEndPoint endPoint, ISocketReaderWriter clientSocketReaderWriter);

        Task Stop();
    }
}
