using Projector.IO.Protocol.Commands;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Projector.IO.Client
{
    public class SubscriptionManager
    {

        private readonly ConcurrentBag<string> _subscriptions = new ConcurrentBag<string>();
        private readonly Client _client;
        private bool _connectionInitialized = false;

        public SubscriptionManager(Client client)
        {
            _client = client;
            _client.OnClientDisconnected += _client_OnClientDisconnected;
        }

        async void _client_OnClientDisconnected(object sender, Client.ClientDisconnectedEventArgs e)
        {
            await Task.Delay(10000);
            await _client.ConnectAsync();

            foreach (var subscription in _subscriptions)
            {
                var subscribeCommand = new SubscribeCommand(subscription);
                await _client.SendCommand(subscribeCommand);
            }
        }

        public async Task Subscribe(string tableName)
        {
            if (!_connectionInitialized)
            {
                await _client.ConnectAsync();
                _connectionInitialized = true;
            }

            var subscribeCommand = new SubscribeCommand(tableName);
            await _client.SendCommand(subscribeCommand);
            _subscriptions.Add(tableName);
        }

        public async Task Unsubscribe(string tableName)
        {
            var subscribeCommand = new SubscribeCommand(tableName);
            await _client.SendCommand(subscribeCommand);
            _subscriptions.Add(tableName);
        }
    }

}
