using Projector.IO.Client;
using Projector.IO.Protocol.Commands;
using Projector.IO.Server;
using System;

namespace Projector.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();

            var startedServerTask = server.Start();

            var subscribeCommand = new SubscribeCommand("table1");
            var client = new Client();
            client.ConnectAsync().Wait();

            client.SendCommand(subscribeCommand).Wait();

            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }


    }
}
