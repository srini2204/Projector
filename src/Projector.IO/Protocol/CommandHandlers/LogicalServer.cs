using Projector.IO.Protocol.Responses;
using Projector.IO.SocketHelpers;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace Projector.IO.Protocol.CommandHandlers
{
    class LogicalServer : ILogicalServer
    {
        private static readonly OkResponse OkResponse = new OkResponse();
        private static readonly Heartbeat Heartbeat = new Heartbeat();

        private readonly Timer _keepAliveTimer;

        private readonly ConcurrentDictionary<IPEndPoint, SocketWrapper> _clients = new ConcurrentDictionary<IPEndPoint, SocketWrapper>();

        public LogicalServer()
        {
            _keepAliveTimer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            _keepAliveTimer.Elapsed += _keepAliveTimer_Elapsed;
            _keepAliveTimer.Start();
        }

        async void _keepAliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var client in _clients)
            {
                await client.Value.SendAsync(Heartbeat.GetBytes());
            }
        }

        public Task<byte[]> ProcessRequestAsync(byte[] data)
        {
            return Task.FromResult(OkResponse.GetBytes());
        }



        public Task RegisterConnectedClient(IPEndPoint endPoint, SocketWrapper socketWrapper)
        {
            _clients.TryAdd(endPoint, socketWrapper);
            return Task.FromResult(0);
        }

        public Task ClientDiconnected(IPEndPoint endPoint)
        {
            SocketWrapper socketWrapper;
            _clients.TryRemove(endPoint, out socketWrapper);
            return Task.FromResult(0);
        }
    }
}
