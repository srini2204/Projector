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
            var startedServerTask = server.StartListen();

            var socketClientSettings = new SocketClientSettings(new IPEndPoint(IPAddress.Loopback, 4444), 4, 25, 4, 10);
            var list = new List<Client>(10000);
            for (int i = 0; i < 10000; i++)
            {
                var client = new Client(socketClientSettings);
                list.Add(client);
                client.ConnectAsync();
                //var subscribeCommand = new SubscribeCommand("table1");
                //client.SendAsync(subscribeCommand.GetBytes());
            }

            Console.ReadKey();

            var subscribeCommand = new SubscribeCommand("table1");

            foreach (var client in list)
            {
                client.SendAsync(subscribeCommand.GetBytes());
            }

            //var subscribeCommand = new SubscribeCommand("table1");
            //client.SendAsync(subscribeCommand.GetBytes()).Wait();
            //client.ReceiveAsync().Wait();
            //client.DisconnectAsync().Wait();

            Console.ReadKey();
        }

        static void server_OnClientDisconnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine("Client from: " + e.EndPoint.Address + ":" + e.EndPoint.Port + " disconnected");
        }

        static void server_OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine("Client connected from: " + e.EndPoint.Address + ":" + e.EndPoint.Port);
        }
    }
}
