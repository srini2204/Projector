using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Projector.IO.SocketHelpers;

namespace Projector.IO
{
    public class Client
    {
        private readonly string _host;
        private readonly int _port;
        private Socket _socket;

        public Client(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task Connect()
        {
            var ipHostInfo = Dns.GetHostEntry(_host);
            var ipAddress = ipHostInfo.AddressList[0];
            var remoteEp = new IPEndPoint(ipAddress, _port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var eventArgs = new SocketAsyncEventArgs { RemoteEndPoint = remoteEp };
            await _socket.AcceptAsync(new SocketAwaitable(eventArgs));
        }
    }
}
