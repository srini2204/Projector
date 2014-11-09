using Projector.IO.SocketHelpers;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Protocol.CommandHandlers
{
    public interface ILogicalServer
    {
        Task RegisterConnectedClient(IPEndPoint endPoint, ISocketReaderWriter clientSocketReaderWriter);

        Task<bool> ProcessRequestAsync(ISocketReaderWriter clientSocketReaderWriter, Stream inputStream);

        Task ClientDiconnected(IPEndPoint endPoint);

        Task Stop();
    }
}
