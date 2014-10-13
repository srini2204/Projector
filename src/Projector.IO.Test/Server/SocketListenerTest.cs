using NUnit.Framework;
using Projector.IO.Client;
using Projector.IO.Server;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Test.Server
{
    [TestFixture]
    class SocketListenerTest
    {
        [Test]
        public async Task TestNewClientAwaiting()
        {
            var socketListener = new SocketListener();
            socketListener.StartListen(new IPEndPoint(IPAddress.Loopback, 4441), 1);

            var taskSocket = socketListener.TakeNewClient();

            Assert.False(taskSocket.IsCompleted, "Task must not be completed. We are waiting for the connection here");

            var socketConnector = new SocketConnector();
            var clientSocket = await socketConnector.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 4441));

            Assert.NotNull(clientSocket, "Corresponding client socket must not be null");

            Assert.True(taskSocket.IsCompleted, "Task must be completed. The first connector should be here already");
            Assert.NotNull(taskSocket.Result, "Corresponding server socket must not be null");

            socketListener.StopListen();
        }

        [Test]
        [ExpectedException(typeof(SocketException))]
        public async Task TestStopListening()
        {
            var socketListener = new SocketListener();
            socketListener.StartListen(new IPEndPoint(IPAddress.Loopback, 4441), 1);

            socketListener.StopListen();

            // try to connect
            var socketConnector = new SocketConnector();
            await socketConnector.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 4441));
        }

        [Test]
        public void TestStopListeningWhileWaitingForNewClient()
        {
            var socketListener = new SocketListener();
            socketListener.StartListen(new IPEndPoint(IPAddress.Loopback, 4441), 1);

            var taskSocket = socketListener.TakeNewClient();

            socketListener.StopListen();

            Assert.Null(taskSocket.Result, "Corresponding server socket must be null");
        }
    }
}
