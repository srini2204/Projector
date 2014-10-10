using Projector.IO.SocketHelpers;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Protocol.CommandHandlers
{
    interface ILogicalServer
    {
        Task RegisterConnectedClient(IPEndPoint endPoint, SocketWrapper socketWrapper);

        Task ProcessRequestAsync(Stream inputStream, Stream outputStream);

        Task ClientDiconnected(IPEndPoint endPoint);
    }
}
