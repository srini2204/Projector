using Projector.IO.Protocol.Commands;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Client
{
    public class Client
    {
        private readonly SocketClient _socketClient;

        public Client()
        {
            var socketClientSettings = new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 25, 10);
            _socketClient = new SocketClient(socketClientSettings);
        }

        public async Task SendCommand(ICommand command)
        {
            await _socketClient.SendAsync(command.GetBytes());

            var res = await _socketClient.ReceiveAsync();
        }

        public async Task ConnectAsync()
        {
            await _socketClient.ConnectAsync();

            //Task.Run(async () =>
            //{
            //    var res = await _socketClient.ReceiveAsync();

            //});

        }

        public Task DisconnectAsync()
        {
            return _socketClient.DisconnectAsync();
        }
    }
}
