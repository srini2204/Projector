using Projector.IO.SocketHelpers;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Protocol.CommandHandlers
{
    interface ILogicalServer
    {
        Task RegisterConnectedClient(IPEndPoint endPoint, SocketWrapper socketWrapper);
        Task<byte[]> ProcessRequestAsync(byte[] data);
        Task ClientDiconnected(IPEndPoint endPoint);
    }
}
