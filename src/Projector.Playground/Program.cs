using Projector.IO.Client;
using Projector.IO.Protocol.Commands;
using Projector.IO.Server;
using System;
using System.Net;

namespace Projector.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverConfig = new SocketListenerSettings(10, 1, 100, 10, 4, 25, 4, 10, new IPEndPoint(IPAddress.Any, 4444));
            var server = new Server(serverConfig);
            var startedServerTask = server.StartListen();

            var socketClientSettings = new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 25, 4, 10);
            var client = new Client(socketClientSettings);
            client.ConnectAsync().Wait();

            var subscribeCommand = new SubscribeCommand("table1");
            client.SendAsync(subscribeCommand.GetBytes()).Wait();
            client.ReceiveAsync().Wait();

            Console.ReadKey();
        }
    }
}
