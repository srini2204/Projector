using Projector.Data;
using Projector.IO.Implementation.Protocol;
using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.SocketHelpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace Projector.IO.Implementation.Server
{
    public class LogicalServer : ILogicalServer
    {
        private static readonly OkResponse OkResponse = new OkResponse();
        private static readonly Heartbeat Heartbeat = new Heartbeat();

        private Dictionary<string, IDataProvider> _tables;

        private readonly Timer _keepAliveTimer;
        private readonly Stream _outputStream;

        private readonly ConcurrentDictionary<IPEndPoint, SocketWrapper> _clients = new ConcurrentDictionary<IPEndPoint, SocketWrapper>();

        public LogicalServer()
        {
            _outputStream = new MemoryStream();
            _tables = new Dictionary<string, IDataProvider>();
            _keepAliveTimer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            _keepAliveTimer.Elapsed += _keepAliveTimer_Elapsed;
            _keepAliveTimer.Start();
        }

        void _keepAliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var client in _clients)
            {
                //await client.Value.SendAsync(Heartbeat.GetBytes());
            }
        }

        public async Task<bool> ProcessRequestAsync(SocketWrapper clientSocket, Stream inputStream)
        {
            await _outputStream.WriteAsync(OkResponse.GetBytes(), 0, OkResponse.GetBytes().Length);
            return await clientSocket.SendAsync(_outputStream);
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
