using Projector.IO.Protocol.Responses;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Server
{
    public class Server
    {
        private readonly SocketListener _socketListener;

        private readonly Dictionary<IPEndPoint, Socket> _clients = new Dictionary<IPEndPoint, Socket>();

        public Server()
        {
            var serverConfig = new SocketListenerSettings(10000, 1, 100, 10, 4, 25, 4, 10, new IPEndPoint(IPAddress.Any, 4444));
            _socketListener = new SocketListener(serverConfig);
            _socketListener.OnClientConnected += _socketListener_OnClientConnected;
            _socketListener.OnClientDisconnected += _socketListener_OnClientDisconnected;
            _socketListener.OnRequestReceived += _socketListener_OnRequestReceived;
        }

        async void _socketListener_OnRequestReceived(object sender, RequestReceivedEventArgs e)
        {
            await _socketListener.SendAsync(e.SocketAwaitable.EventArgs.AcceptSocket, new OkResponse().GetBytes());
        }

        void _socketListener_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            _clients.Remove(e.EndPoint);
        }

        void _socketListener_OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            _clients.Add(e.EndPoint, e.Socket);
        }

        public Task Start()
        {
            return _socketListener.StartListen();
        }

        public void Stop()
        {

        }
    }
}
