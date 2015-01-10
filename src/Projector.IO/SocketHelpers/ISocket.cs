using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.SocketHelpers
{
    public interface ISocket
    {
        void Close();

        void Shutdown(SocketShutdown socketShutdown);

        Task ReceiveAsync(SocketAwaitable awaitable);

        Task SendAsync(SocketAwaitable awaitable);

        Task DisconnectAsync(SocketAwaitable awaitable);

        EndPoint RemoteEndPoint { get; }
    }
}
