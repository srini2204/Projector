using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.Server;
using System;
using System.Net;

namespace Projector.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server(new SocketListenerSettings(10000, 1, 100, 4, 25, 10, new IPEndPoint(IPAddress.Any, 4444)), new LogicalServer());

            var startedServerTask = server.Start();

            //var subscribeCommand = new SubscribeCommand("table1");
            //var client = new Client();
            //client.ConnectAsync().Wait();

            //for (int i = 0; i < 100000; i++)
            //{
            //   client.SendCommand(subscribeCommand).Wait();
            //}

            //server.Stop();

            startedServerTask.Wait();

            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }


    }
}
