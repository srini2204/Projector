using Projector.Data;
using Projector.IO.Protocol;
using Projector.IO.Utils;
using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.SocketHelpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Projector.IO.Server
{
    public class LogicalServer : ILogicalServer
    {
        private Dictionary<string, IDataProvider> _tables;

        private readonly System.Timers.Timer _keepAliveTimer;

        private readonly ConcurrentDictionary<IPEndPoint, Tuple<ISocketReaderWriter, Stream>> _clients;
        private readonly Dictionary<IPEndPoint, List<IDisconnectable>> _clientSubscriptions;

        private readonly ISyncLoop _syncLoop;

        private readonly CancellationTokenSource _cancellationTokenSource;


        public LogicalServer(ISyncLoop syncLoop)
        {
            _syncLoop = syncLoop;
            _cancellationTokenSource = new CancellationTokenSource();
            _tables = new Dictionary<string, IDataProvider>();
            _clients = new ConcurrentDictionary<IPEndPoint, Tuple<ISocketReaderWriter, Stream>>();
            _clientSubscriptions = new Dictionary<IPEndPoint, List<IDisconnectable>>();
            _keepAliveTimer = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            _keepAliveTimer.Elapsed += _keepAliveTimer_Elapsed;
            _keepAliveTimer.Start();
        }

        public async void Publish(string tableName, IDataProvider dataProvider)
        {
            await _syncLoop.Run(() =>
                {
                    _tables.Add(tableName, dataProvider);
                });

        }

        async void _keepAliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await _syncLoop.Run(() =>
               {
                   foreach (var client in _clients)
                   {
                       MessageComposer.WriteHeartbeatMessage(client.Value.Item2);
                   }
               });
        }





        public async Task RegisterConnectedClient(IPEndPoint endPoint, ISocketReaderWriter clientSocketReaderWriter)
        {

            var clientOutputStream = new CircularStream(100000 * 1024);
            var clientInputStream = new CircularStream(1024);

            _clients.TryAdd(endPoint, new Tuple<ISocketReaderWriter, Stream>(clientSocketReaderWriter, clientOutputStream));

            var readTask = StartReceiveLoop(endPoint, clientSocketReaderWriter, clientInputStream, clientOutputStream);
            var sendTask = StartSendingLoop(clientSocketReaderWriter, clientOutputStream);

            var taskFinished = await Task.WhenAny(readTask, sendTask);

            if (taskFinished == readTask)
            {
                clientOutputStream.WakeItUp();
                await sendTask;
            }
            else
            {

            }

            Tuple<ISocketReaderWriter, Stream> clientInfo;
            _clients.TryRemove(endPoint, out clientInfo);
        }

        private async Task StartSendingLoop(ISocketReaderWriter clientSocketReaderWriter, CircularStream outputStream)
        {
            await Task.Yield();
            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                if (outputStream.Length == 0)
                {
                    if (await outputStream.WaitForData() == 0)
                    {
                        break;
                    }
                }

                if (!await clientSocketReaderWriter.SendAsync(outputStream))
                {
                    break;
                }
            }
        }

        private async Task StartReceiveLoop(IPEndPoint endPoint,
                                            ISocketReaderWriter clientSocketReaderWriter,
                                            Stream clientInputStream,
                                            Stream clientOutputStream)
        {
            await Task.Yield();

            var token = _cancellationTokenSource.Token;

            var messageLength = 0;
            while (!token.IsCancellationRequested && await clientSocketReaderWriter.ReceiveAsync(clientInputStream))
            {
                var enoughBytes = false;
                do
                {
                    if (messageLength == 0 && clientInputStream.Length >= 4)
                    {
                        messageLength = ByteBlader.ReadInt32(clientInputStream);
                    }

                    // check if we have all command already
                    // if so - process
                    if (messageLength > 0 && clientInputStream.Length >= messageLength)
                    {
                        await ParseRequest(endPoint, clientInputStream, messageLength, clientOutputStream);
                        messageLength = 0;

                        if (clientInputStream.Length > 0)
                        {
                            enoughBytes = true;
                        }
                        else
                        {
                            enoughBytes = false;
                        }
                    }
                    else
                    {
                        enoughBytes = false;
                    }

                }
                while (enoughBytes);
            }


            await ClientDiconnected(endPoint);
        }

        private async Task ParseRequest(IPEndPoint endPoint, Stream inputStream, int messageLength, Stream outputStream)
        {
            var bytesLeft = messageLength;
            var subscriptionId = ByteBlader.ReadInt32(inputStream);
            bytesLeft -= 4;

            var commandType = (byte)inputStream.ReadByte();
            bytesLeft--;
            if (Constants.MessageType.Subscribe == commandType)
            {

                var tableName = ByteBlader.ReadString(inputStream, bytesLeft);


                await _syncLoop.Run(() =>
                {
                    IDataProvider dataProvider;
                    if (_tables.TryGetValue(tableName, out dataProvider))
                    {
                        MessageComposer.WriteOkMessage(outputStream, subscriptionId);

                        var networkAdapter = new NetworkAdapter(outputStream, subscriptionId);
                        List<IDisconnectable> clientSubscriptions;
                        if (!_clientSubscriptions.TryGetValue(endPoint, out clientSubscriptions))
                        {
                            clientSubscriptions = new List<IDisconnectable>();
                            _clientSubscriptions.Add(endPoint, clientSubscriptions);
                        }

                        var disconnectable = dataProvider.AddConsumer(networkAdapter);
                        clientSubscriptions.Add(disconnectable);
                    }
                    else
                    {
                        MessageComposer.WriteOkMessage(outputStream, subscriptionId);
                    }
                });
            }

        }


        private Task ClientDiconnected(IPEndPoint endPoint)
        {
            return _syncLoop.Run(() =>
                {
                    List<IDisconnectable> clientSubscriptions;
                    if (_clientSubscriptions.TryGetValue(endPoint, out clientSubscriptions))
                    {
                        foreach (var disconnactable in clientSubscriptions)
                        {
                            disconnactable.Dispose();
                        }
                        _clientSubscriptions.Remove(endPoint);
                    }
                });
        }


        public async Task Stop()
        {
            _cancellationTokenSource.Cancel();

            while (!_clients.IsEmpty)
            {
                await Task.Delay(100);
            }
        }
    }
}
