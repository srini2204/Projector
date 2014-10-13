using Projector.IO.SocketHelpers;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Protocol.CommandHandlers
{
    public interface ILogicalServer
    {
        Task RegisterConnectedClient(IPEndPoint endPoint, SocketWrapper socketWrapper);

        Task<bool> ProcessRequestAsync(SocketWrapper clientSocket, Stream inputStream);

        Task ClientDiconnected(IPEndPoint endPoint);
    }
}
