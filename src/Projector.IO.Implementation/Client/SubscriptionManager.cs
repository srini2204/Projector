using Projector.Data;
using Projector.Data.Tables;
using Projector.IO.Implementation.Protocol;
using Projector.IO.Implementation.Utils;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Client
{
    public class SubscriptionManager
    {
        private readonly ConcurrentBag<string> _subscriptions = new ConcurrentBag<string>();
        private readonly ConcurrentDictionary<int, Table> _tables = new ConcurrentDictionary<int, Table>();
        private readonly Projector.IO.Client.Client _client;
        private bool _connectionInitialized = false;
        private readonly ISyncLoop _syncLoop;
        private AutoResetEvent _eventNotifier = new AutoResetEvent(false);

        public SubscriptionManager(Projector.IO.Client.Client client, ISyncLoop syncLoop)
        {
            _client = client;
            _syncLoop = syncLoop;
            //_client.OnClientDisconnected += _client_OnClientDisconnected;
        }

        async void _client_OnClientDisconnected(object sender, Projector.IO.Client.Client.ClientDisconnectedEventArgs e)
        {
            await Task.Delay(10000);
            await _client.ConnectAsync();

            foreach (var subscription in _subscriptions)
            {
                using (var memoryStream = new MemoryStream())
                {
                    MessageComposer.WriteSubscribeMessage(memoryStream, 0, subscription);
                    memoryStream.Position = 0;
                    await _client.SendCommand(memoryStream);
                }
            }
        }

        public async Task<IDataProvider> Subscribe(string tableName)
        {
            if (!_connectionInitialized)
            {
                await _client.ConnectAsync();
                _connectionInitialized = true;
                StartReadingFromServer();
            }

            using (var memoryStream = new MemoryStream())
            {
                MessageComposer.WriteSubscribeMessage(memoryStream, 0, tableName);
                memoryStream.Position = 0;
                await _client.SendCommand(memoryStream);
            }

            _eventNotifier.WaitOne();
            return _tables[0];
        }

        public async Task Unsubscribe(string tableName)
        {

            await _client.SendCommand(null);
            _subscriptions.Add(tableName);
        }

        private async void StartReadingFromServer()
        {
            try
            {
                using (var inputStream = new CircularStream(1024 * 100000))
                {

                    var messageLength = 0;
                    while (await _client.ReadAsync(inputStream)) // !token.IsCancellationRequested &&
                    {
                        var enoughBytes = false;
                        do
                        {
                            if (messageLength == 0 && inputStream.Length >= 4)
                            {
                                messageLength = ByteBlader.ReadInt32(inputStream);
                            }

                            // check if we have all command already
                            // if so - process
                            if (messageLength > 0 && inputStream.Length >= messageLength)
                            {
                                await ParseRequest(inputStream, messageLength);
                                messageLength = 0;
                                if (inputStream.Length > 0)
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
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private async Task ParseRequest(CircularStream inputStream, int messageLength)
        {
            var bytesLeft = messageLength;
            var subscriptionId = ByteBlader.ReadInt32(inputStream);
            bytesLeft -= 4;

            var commandType = (byte)ByteBlader.ReadByte(inputStream);
            bytesLeft--;
            if (Constants.MessageType.Ok == commandType)
            {

            }
            else if (Constants.MessageType.Schema == commandType)
            {
                var schema = new Schema(10);
                while (bytesLeft > 0)
                {
                    var type = ByteBlader.ReadByte(inputStream);
                    bytesLeft--;
                    var nameLength = ByteBlader.ReadInt32(inputStream);
                    bytesLeft -= 4;
                    var fieldName = ByteBlader.ReadString(inputStream, nameLength);
                    bytesLeft -= nameLength;

                    if (Constants.FieldType.Int == type)
                    {
                        schema.CreateField<int>(fieldName);
                    }
                    else if (Constants.FieldType.Long == type)
                    {
                        schema.CreateField<long>(fieldName);
                    }
                    else if (Constants.FieldType.String == type)
                    {
                        schema.CreateField<string>(fieldName);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown field type: " + type);
                    }
                }

                var table = new Table(schema);
                _tables.TryAdd(subscriptionId, table);
                
            }
            else if (Constants.MessageType.RowAdded == commandType)
            {
                var table = _tables[subscriptionId];
                await _syncLoop.Run(() =>
                    {
                        var newRowId = table.NewRow();
                        while (bytesLeft > 0)
                        {
                            var columnId = ByteBlader.ReadInt32(inputStream);
                            bytesLeft -= 4;
                            var iField = table.Schema.Columns[columnId];

                            if (iField.DataType == typeof(int))
                            {
                                var value = ByteBlader.ReadInt32(inputStream);
                                table.Set<int>(newRowId, iField.Name, value);
                                bytesLeft -= 4;
                            }
                            else if (iField.DataType == typeof(long))
                            {
                                var value = ByteBlader.ReadLong(inputStream);
                                table.Set<long>(newRowId, iField.Name, value);
                                bytesLeft -= 8;
                            }
                            else if (iField.DataType == typeof(string))
                            {
                                var stringValueLength = ByteBlader.ReadInt32(inputStream);
                                bytesLeft -= 4;
                                var stringValue = ByteBlader.ReadString(inputStream, stringValueLength);
                                table.Set<string>(newRowId, iField.Name, stringValue);
                                bytesLeft -= stringValueLength;
                            }
                        }
                    });

            }
            else if (Constants.MessageType.SyncPoint == commandType)
            {
                await _syncLoop.Run(() =>
                    {
                        _tables[subscriptionId].FireChanges();
                        _eventNotifier.Set();
                    });
            }
            else
            {
                throw new InvalidOperationException("Unknown command type: " + commandType);
            }

            if (bytesLeft > 0)
            {
                throw new InvalidOperationException("There are bytes left after parsing. Should be zero");
            }

        }
    }

}
