using Projector.IO.Client;
using Projector.IO.Protocol.Commands;
using Projector.IO.Server;
using System;
using System.Collections.Generic;
using System.Net;

namespace Projector.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();

            var startedServerTask = server.Start();

            var socketClientSettings = new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 25, 4, 10);


            var subscribeCommand = new SubscribeCommand("table1");
            var client = new Client(socketClientSettings);
            client.ConnectAsync().Wait();
            client.SendAsync(subscribeCommand.GetBytes()).Wait();
            client.ReceiveAsync().Wait();


            

            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }

        
    }
}
