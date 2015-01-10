using NUnit.Framework;
using Projector.IO.Client;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Test.Client
{
    [TestFixture]
    class SocketConnectorTest
    {

        [Test]
        [ExpectedException(typeof(SocketException))]
        public async Task TestConnectionRefused()
        {
            var socketConnector = new SocketConnector();
            await socketConnector.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 4441));
        }

        [Test]
        public async Task TestSuccessfulConnection()
        {
            // prepare socket listener
            var localEndPoint = new IPEndPoint(IPAddress.Any, 4441);
            var listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.Bind(localEndPoint);

            listenSocket.Listen(1);

            // test
            var socketConnector = new SocketConnector();
            var socket = await socketConnector.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 4441));

            Assert.True(socket.Connected);
        }

    }
}
