using Projector.IO.Client;
using Projector.IO.Implementation.Client;
using System;
using System.Diagnostics;
using System.Net;

namespace Projector.Client
{
    class Program
    {
        static void Main(string[] args)
        {

            var client = new Projector.IO.Client.Client(new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 25, 10));

            var subscriptionManager = new SubscriptionManager(client);
            Console.WriteLine("Connected. Press any key...");

            while (Console.ReadKey().Key == ConsoleKey.Spacebar)
            {
                var stopwatch = Stopwatch.StartNew();
                subscriptionManager.Subscribe("table1").Wait();
                stopwatch.Stop();
                Console.WriteLine("Ok. Took: " + stopwatch.Elapsed);
            }


            
            Console.ReadKey();
        }
    }
}
