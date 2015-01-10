using Projector.IO.SocketHelpers;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Server
{
    public interface ISocketListener
    {
        Task<ISocket> TakeNewClient();

        void StartListen(IPEndPoint iPEndPoint, int backlog);

        void StopListen();
    }
}
