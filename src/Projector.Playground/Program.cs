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


            var list = new List<Client>(1);
            for (int i = 0; i < 10000; i++)
            {
                var client = new Client(socketClientSettings);
                list.Add(client);
                client.ConnectAsync().Wait();
            }


            Console.WriteLine("Connected. Press any key to send requests. Press Enter to quit");


            int k = 0;

            while (Console.ReadKey().Key != ConsoleKey.Enter)
            {
                for (int i = 0; i < 100; i++)
                {
                    foreach (var client in list)
                    {

                        client.SendAsync(subscribeCommand.GetBytes());
                        k++;
                    }
                    Console.WriteLine("Client sent " + i);
                }
                Console.WriteLine("Press any key to send requests. Press Enter to quit");
            }

            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }

        static void server_OnRequestReceived(object sender, RequestReceivedEventArgs e)
        {
            //Console.WriteLine("Server: client from: " + e.EndPoint.Address + ":" + e.EndPoint.Port + " sent some data");
        }

        static void server_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Server: Client from: " + e.EndPoint.Address + ":" + e.EndPoint.Port + " disconnected");
        }

        static void server_OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine("Server: Client connected from: " + e.EndPoint.Address + ":" + e.EndPoint.Port);
        }
    }
}
