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
        private static readonly OkResponse OkResponse = new OkResponse();
        private static readonly Heartbeat Heartbeat = new Heartbeat();

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

        void _keepAliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var client in _clients)
            {
                //await client.Value.SendAsync(Heartbeat.GetBytes());
            }
        }

        public async Task<bool> ProcessRequestAsync(ISocketReaderWriter clientSocketReaderWriter, Stream inputStream)
        {
            var clientOutputStream = (Stream)clientSocketReaderWriter.Token;
            await ParseRequest(inputStream, clientOutputStream);
            return true;
        }

        private async Task ParseRequest(Stream inputStream, Stream outputStream)
        {
            if (inputStream.Length >= 4)
            {
                var messageLength = ByteBlader.ReadInt32(inputStream);
                if (inputStream.Length >= messageLength - 4)
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
                                    outputStream.WriteAsync(OkResponse.GetBytes(), 0, OkResponse.GetBytes().Length);
                                }
                                else
                                {
                                    outputStream.WriteAsync(OkResponse.GetBytes(), 0, OkResponse.GetBytes().Length);
                                }
                            });
                    }
                }
                else
                {
                    inputStream.Position -= 4;
                }
            }

            if (inputStream.Length == inputStream.Position)
            {
                inputStream.Position = 0;
                inputStream.SetLength(0);
            }
        }



        public Task RegisterConnectedClient(IPEndPoint endPoint, ISocketReaderWriter clientSocketReaderWriter)
        {
            _clients.TryAdd(endPoint, clientSocketReaderWriter);
            var outputStream = new CircularStream(1000 * 1024);

            clientSocketReaderWriter.Token = outputStream;


            StartSendingLoop(clientSocketReaderWriter, outputStream);
            return Task.FromResult(0);
        }

        private async void StartSendingLoop(ISocketReaderWriter clientSocketReaderWriter, CircularStream outputStream)
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

        public Task ClientDiconnected(IPEndPoint endPoint)
        {
            ISocketReaderWriter clientSocketReaderWriter;
            _clients.TryRemove(endPoint, out clientSocketReaderWriter);
            return Task.FromResult(0);
        }


        public Task Stop()
        {
            _cancellationTokenSource.Cancel();
            return Task.FromResult(0);
        }
    }
}
