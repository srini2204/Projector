using Projector.Data;
using Projector.IO.Implementation.Server;
using Projector.IO.Implementation.Utils;
using Projector.IO.Server;
using System;
using System.Net;
using System.Threading;

namespace Projector.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var syncLoop = new SyncLoop();
            var syncLoopTask = syncLoop.StartProcessActions(CancellationToken.None);
            var logicalServer = new LogicalServer(syncLoop);
            var server = new Server(new SocketListenerSettings(10000, 1, 100, 4, 25, 10, new IPEndPoint(IPAddress.Any, 4444)), logicalServer, new SocketListener());

            var schema = new Schema(10);
            schema.CreateField<int>("TestInt");

            var table = new Table(schema);
            logicalServer.Publish("table1",table);

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
            syncLoopTask.Wait();

            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }


    }
}
