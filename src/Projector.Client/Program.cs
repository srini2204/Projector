using Projector.Data;
using Projector.IO.Client;
using Projector.IO.Client;
using Projector.IO.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Projector.Client
{
    class Program
    {
        static void Main(string[] args)
        {

            var client = new Projector.IO.Client.Client(new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 1024 * 1000, 10));

            var syncLoop = new SyncLoop();
            syncLoop.StartProcessActions(CancellationToken.None);
            var subscriptionManager = new SubscriptionManager(client, syncLoop);
            Console.WriteLine("Connected. Press any key...");


            var table = subscriptionManager.Subscribe("table1").Result;
            Console.WriteLine("Ok.");

            syncLoop.Run(() =>
                {
                    table.AddConsumer(new ConsoleConsumer());
                }
            );


            Console.ReadKey();
            client.DisconnectAsync().Wait();
        }

        private class ConsoleConsumer : IDataConsumer
        {
            private int _rowCounter;
            private ISchema _schema;

            public void OnAdd(IList<int> ids)
            {
                _rowCounter += ids.Count;
                Console.WriteLine(DateTime.Now + " OnAdd arrived. Row count: " + ids.Count + " Total count: " + _rowCounter);
                
            }

            public void OnUpdate(IList<int> ids, IList<IField> updatedFields)
            {
                Console.WriteLine("OnUpdate arrived");
            }

            public void OnDelete(IList<int> ids)
            {
                Console.WriteLine("OnDelete arrived");
            }

            public void OnSchema(ISchema schema)
            {
                _schema = schema;
                Console.WriteLine("Schema arrived");

                foreach (var column in schema.Columns)
                {
                    Console.WriteLine("Field: " + column.Name + " of type: " + column.DataType);
                }
            }

            public void OnSyncPoint()
            {
                Console.WriteLine("SyncPoint arrived");
            }
        }

    }
}
