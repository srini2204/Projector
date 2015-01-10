using Projector.Data;
using Projector.IO.Implementation.Server;
using Projector.IO.Implementation.Utils;
using Projector.IO.Server;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var syncLoop = new SyncLoop();
            var syncLoopTask = syncLoop.StartProcessActions(CancellationToken.None);
            var logicalServer = new LogicalServer(syncLoop);
            var server = new Server(new SocketListenerSettings(10000, 1, 100, 4, 1024*8, 8, new IPEndPoint(IPAddress.Any, 4444)), logicalServer, new SocketListener());

            var schema = new Schema(1000000);
            schema.CreateField<int>("Age");
            schema.CreateField<string>("Name");
            schema.CreateField<long>("Time");
            schema.CreateField<long>("Time1");

            var table = new Table(schema);
            logicalServer.Publish("table1", table);

            var startedServerTask = server.Start();

            Task.Run(async () =>
            {
                await syncLoop.Run(() =>
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        var rowId1 = table.NewRow();
                        table.Set<int>(rowId1, "Age", i);
                        table.Set<string>(rowId1, "Name", "Max" + i);
                        table.Set<long>(rowId1, "Time", 125000+i);
                        table.Set<long>(rowId1, "Time1", i);
                    }

                    table.FireChanges();

                    Console.WriteLine("Published");
                });

                while (true)
                {
                    await Task.Delay(1000);
                    await syncLoop.Run(() =>
                        {
                            var rowId1 = table.NewRow();
                            table.Set<int>(rowId1, "Age", 25);
                            table.Set<string>(rowId1, "Name", "Max");
                            table.Set<long>(rowId1, "Time", 125000);
                            table.Set<long>(rowId1, "Time1", 1);


                            table.FireChanges();
                        });

                }
            });

            startedServerTask.Wait();
            syncLoopTask.Wait();

            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }


    }
}
