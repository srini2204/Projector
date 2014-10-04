using Projector.IO.Protocol.Commands;
using System;

namespace Projector.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new IO.Client.Client();
            client.ConnectAsync().Wait();
            var subscribeCommand = new SubscribeCommand("table1");
            client.SendCommand(subscribeCommand).Wait();

            Console.WriteLine("Connected. Press any key...");
            Console.ReadKey();
        }
    }
}
