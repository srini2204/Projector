using Projector.IO.Client;
using Projector.IO.Protocol.Commands;
using System;
using System.Diagnostics;

namespace Projector.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var subscriptionManager = new SubscriptionManager();
            
            while (Console.ReadKey().Key == ConsoleKey.Spacebar)
            {
                var stopwatch=Stopwatch.StartNew();
                subscriptionManager.Subscribe("table1").Wait();
                stopwatch.Stop();
                Console.WriteLine("Ok. Took: " + stopwatch.Elapsed);
            }
            

            Console.WriteLine("Connected. Press any key...");
            Console.ReadKey();
        }
    }
}
