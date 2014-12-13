using Projector.Data;
using Projector.IO.Implementation.Protocol;
using Projector.IO.Implementation.Utils;
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

namespace Projector.IO.Implementation.Server
{
    public class LogicalServer : ILogicalServer
    {
        private Dictionary<string, IDataProvider> _tables;

        private readonly System.Timers.Timer _keepAliveTimer;

        private readonly ConcurrentDictionary<IPEndPoint, ISocketReaderWriter> _clients;

        private readonly ISyncLoop _syncLoop;

        private readonly CancellationTokenSource _cancellationTokenSource;


        public LogicalServer(ISyncLoop syncLoop)
        {
            _syncLoop = syncLoop;
            _cancellationTokenSource = new CancellationTokenSource();
            _tables = new Dictionary<string, IDataProvider>();
            _clients = new ConcurrentDictionary<IPEndPoint, ISocketReaderWriter>();
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
                       //MessageComposer.WriteHeartbeatMessage(client.)
                   }
               });
        }





        public Task RegisterConnectedClient(IPEndPoint endPoint, ISocketReaderWriter clientSocketReaderWriter)
        {
            _clients.TryAdd(endPoint, clientSocketReaderWriter);
            var clientOutputStream = new CircularStream(1000 * 1024);
            var clientInputStream = new CircularStream(1024);

            var readTask = StartReceiveLoop(endPoint, clientSocketReaderWriter, clientInputStream, clientOutputStream);
            var sendTask = StartSendingLoop(clientSocketReaderWriter, clientOutputStream);
            return Task.WhenAll(readTask, sendTask);
        }

        private async Task StartSendingLoop(ISocketReaderWriter clientSocketReaderWriter, CircularStream outputStream)
        {
            await Task.Yield();
            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                if (outputStream.Length == 0)
                {
                    await outputStream.WaitForData();
                }

                await clientSocketReaderWriter.SendAsync(outputStream);
            }
        }

        private async Task StartReceiveLoop(IPEndPoint endPoint,
                                            ISocketReaderWriter clientSocketReaderWriter,
                                            Stream clientInputStream,
                                            Stream clientOutputStream)
        {
            await Task.Yield();

            var token = _cancellationTokenSource.Token;
            var signaledForStopping = false;

            var messageLength = 0;
            while (!token.IsCancellationRequested && !signaledForStopping)
            {
                signaledForStopping = !await clientSocketReaderWriter.ReceiveAsync(clientInputStream);

                if (!signaledForStopping)
                {
                    if (messageLength == 0 && clientInputStream.Length >= 4)
                    {
                        messageLength = ByteBlader.ReadInt32(clientInputStream);
                    }

                    // check if we have all command already
                    // if so - process
                    if (clientInputStream.Length >= messageLength)
                    {
                        await ParseRequest(clientInputStream, messageLength, clientOutputStream);
                        messageLength = 0;
                    }
                }
            }


            await ClientDiconnected(endPoint);

        }

        private async Task ParseRequest(Stream inputStream, int messageLength, Stream outputStream)
        {
            var commandType = (byte)inputStream.ReadByte();
            if (Constants.MessageType.Subscribe == commandType)
            {
                var bytesForString = new byte[messageLength - 1];
                await inputStream.ReadAsync(bytesForString, 0, bytesForString.Length);
                var tableName = Encoding.ASCII.GetString(bytesForString);

                await _syncLoop.Run(() =>
                {
                    IDataProvider dataProvider;
                    if (_tables.TryGetValue(tableName, out dataProvider))
                    {
                        MessageComposer.WriteOkMessage(outputStream);

                        dataProvider.AddConsumer(new NetworkAdapter(outputStream, 0));
                    }
                    else
                    {
                        MessageComposer.WriteOkMessage(outputStream);
                    }
                });
            }

        }


        private Task ClientDiconnected(IPEndPoint endPoint)
        {
            ISocketReaderWriter clientSocketReaderWriter;
            _clients.TryRemove(endPoint, out clientSocketReaderWriter);
            return Task.FromResult(0);
        }


        public async Task Stop()
        {
            _cancellationTokenSource.Cancel();

            var taskList = new List<Task>();
            foreach (var socket in _clients)
            {
                taskList.Add(socket.Value.DisconnectAsync());
            }

            await Task.WhenAll(taskList);

            while (!_clients.IsEmpty)
            {
                await Task.Delay(100);
            }
        }
    }
}
