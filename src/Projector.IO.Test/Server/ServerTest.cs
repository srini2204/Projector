using NSubstitute;
using NUnit.Framework;
using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.Server;
using Projector.IO.SocketHelpers;
using Projector.IO.Test.TestHelpers;
using System.Net;
using System.Threading.Tasks;
using System;

namespace Projector.IO.Test.Server
{
    [TestFixture]
    class ServerTest
    {
        private Projector.IO.Server.Server _server;
        private ILogicalServer _mockLogicalServer;
        private ISocketListener _mockSocketListener;

        private IPEndPoint _ipEndPoint;

        private TaskCompletionSource<ISocket> _taskCompletionSourceStopNotifier;

        [SetUp]
        public void InitContext()
        {
            _mockLogicalServer = Substitute.For<ILogicalServer>();
            _mockSocketListener = Substitute.For<ISocketListener>();

            _ipEndPoint = new IPEndPoint(IPAddress.Any, 4441);

            _taskCompletionSourceStopNotifier = new TaskCompletionSource<ISocket>();
            _mockSocketListener.When(x => x.StopListen()).Do(x => _taskCompletionSourceStopNotifier.SetResult(null));

            _server = new Projector.IO.Server.Server(new SocketListenerSettings(10, 1, 10, 4, 25, 10, _ipEndPoint), _mockLogicalServer, _mockSocketListener);
        }

        [Test]
        public void TestServerStart()
        {
            //set up
            _mockSocketListener.TakeNewClient().Returns(_taskCompletionSourceStopNotifier.Task);

            //execute
            _server.Start();

            //check
            _mockSocketListener.Received(1).StartListen(_ipEndPoint, 10);

            _mockSocketListener.Received(1).TakeNewClient().Forget();

            _mockLogicalServer.DidNotReceive().RegisterConnectedClient(Arg.Any<IPEndPoint>(), Arg.Any<ISocketReaderWriter>()).Forget();
        }

        [Test]
        public async Task TestServerStopWhileNoActiveConnections()
        {
            //set up
            _mockSocketListener.TakeNewClient().Returns(_taskCompletionSourceStopNotifier.Task);

            //execute
            _server.Start();

            var taskStopServer = _server.Stop();

            await taskStopServer;

            //check
            _mockLogicalServer.DidNotReceive().RegisterConnectedClient(Arg.Any<IPEndPoint>(), Arg.Any<ISocketReaderWriter>()).Forget();
        }

        [Test]
        public void TestServerStopWhileThereActiveConnections()
        {
            throw new NotImplementedException();
        }

        [Test]
        public async Task TestNewClientConnected()
        {
            //set up
            var socketReadReceived = new TaskCompletionSource<int>();
            ISocket socket = Substitute.For<ISocket>();
            socket.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Any, 44412));

            _mockLogicalServer.RegisterConnectedClient(Arg.Any<IPEndPoint>(),
                Arg.Do<ISocketReaderWriter>(x => socketReadReceived.SetResult(0))).Forget();
            

            _mockSocketListener.TakeNewClient().Returns(Task.FromResult(socket), _taskCompletionSourceStopNotifier.Task);

            //execute
            _server.Start();

            await socketReadReceived.Task;

            //check
            

            _mockSocketListener.Received(2).TakeNewClient().Forget();

            _mockLogicalServer.Received(1).RegisterConnectedClient((IPEndPoint)socket.RemoteEndPoint, Arg.Any<ISocketReaderWriter>()).Forget();
        }
    }
}
