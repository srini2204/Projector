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
            var serverConfig = new SocketListenerSettings(10000, 1, 100, 10, 4, 25, 4, 10, new IPEndPoint(IPAddress.Any, 4444));
            var server = new Server(serverConfig);
            server.OnClientConnected += server_OnClientConnected;
            server.OnClientDisconnected += server_OnClientDisconnected;
            server.OnRequestReceived += server_OnRequestReceived;
            var startedServerTask = server.StartListen();

            var socketClientSettings = new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 25, 4, 10);

            
            var subscribeCommand = new SubscribeCommand("table1");
            

            var list = new List<Client>(1);
            for (int i = 0; i < 1; i++)
            {
                var client = new Client(socketClientSettings);
                list.Add(client);
                client.ConnectAsync();
                //var subscribeCommand = new SubscribeCommand("table1");
                //client.SendAsync(subscribeCommand.GetBytes());
            }





            int k = 0;

            while (Console.ReadKey().Key != ConsoleKey.Enter)
            {
                foreach (var client in list)
                {
                    k++;
                    Console.WriteLine("Client sent " + k);
                    client.SendAsync(subscribeCommand.GetBytes()).Wait();
                }
            }


            //var subscribeCommand = new SubscribeCommand("table1");
            //client.SendAsync(subscribeCommand.GetBytes()).Wait();
            //client.ReceiveAsync().Wait();
            //client.DisconnectAsync().Wait();
            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }

        static void server_OnRequestReceived(object sender, RequestReceivedEventArgs e)
        {
            Console.WriteLine("Server: client from: " + e.EndPoint.Address + ":" + e.EndPoint.Port + " sent some data");
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
