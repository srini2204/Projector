using NSubstitute;
using NUnit.Framework;
using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.Server;
using Projector.IO.SocketHelpers;
using Projector.IO.Test.TestHelpers;
using System.IO;
using System.Net;
using System.Threading.Tasks;

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
            var taskServerRun = _server.Start();

            //check
            Assert.False(taskServerRun.IsCompleted);

            _mockSocketListener.Received(1).StartListen(_ipEndPoint, 10);

            _mockSocketListener.Received(1).TakeNewClient().Forget();

            _mockLogicalServer.DidNotReceive().RegisterConnectedClient(Arg.Any<IPEndPoint>(), Arg.Any<ISocketReaderWriter>()).Forget();

            _mockLogicalServer.DidNotReceive().ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Forget();

            _mockLogicalServer.DidNotReceive().ClientDiconnected(Arg.Any<IPEndPoint>()).Forget();
        }

        [Test]
        public async Task TestServerStopWhileNoActiveConnections()
        {
            //set up
            _mockSocketListener.TakeNewClient().Returns(_taskCompletionSourceStopNotifier.Task);

            //execute
            var taskServerRun = _server.Start();

            _server.Stop();

            await taskServerRun;

            //check
            _mockLogicalServer.DidNotReceive().RegisterConnectedClient(Arg.Any<IPEndPoint>(), Arg.Any<ISocketReaderWriter>()).Forget();

            _mockLogicalServer.DidNotReceive().ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Forget();

            _mockLogicalServer.DidNotReceive().ClientDiconnected(Arg.Any<IPEndPoint>()).Forget();
        }

        [Test]
        public async Task TestServerStopWhileThereActiveConnections()
        {
            //set up

            Assert.Fail();
        }

        [Test]
        public async Task TestNewClientConnected()
        {
            //set up
            var socketReadReceived = new TaskCompletionSource<int>();
            ISocket socket = Substitute.For<ISocket>();
            socket.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Any, 44412));
            socket.ReceiveAsync(Arg.Do<SocketAwaitable>(x => socketReadReceived.SetResult(0))).Returns(new TaskCompletionSource<int>().Task);

            _mockSocketListener.TakeNewClient().Returns(Task.FromResult(socket), _taskCompletionSourceStopNotifier.Task);

            //execute
            var taskServerRun = _server.Start();

            await socketReadReceived.Task;

            //check
            socket.Received(1).ReceiveAsync(Arg.Any<SocketAwaitable>()).Forget();

            _mockSocketListener.Received(2).TakeNewClient().Forget();

            _mockLogicalServer.Received(1).RegisterConnectedClient((IPEndPoint)socket.RemoteEndPoint, Arg.Any<ISocketReaderWriter>()).Forget();

            _mockLogicalServer.DidNotReceive().ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Forget();

            _mockLogicalServer.DidNotReceive().ClientDiconnected(Arg.Any<IPEndPoint>()).Forget();
        }

        [Test]
        public async Task TestNewClientSentSomeDataForTheServer()
        {
            // set up
            var operationEndedTask = new TaskCompletionSource<int>();
            ISocket socket = Substitute.For<ISocket>();
            socket.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Any, 44412));
            socket.ReceiveAsync(Arg.Do<SocketAwaitable>(x =>
            {
                if (x.BytesTransferred == 0)
                {
                    x.BytesTransferred = 5;// first call
                }
                else
                {
                    operationEndedTask.SetResult(0); // second call
                }
            }
            )).Returns(Task.FromResult(0), new TaskCompletionSource<int>().Task);

            _mockSocketListener.TakeNewClient().Returns(Task.FromResult(socket), _taskCompletionSourceStopNotifier.Task);

            _mockLogicalServer.ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Returns(Task.FromResult(true));

            // execute
            var taskServerRun = _server.Start();

            // wait untill the test is over
            await operationEndedTask.Task;

            // check
            socket.Received(2).ReceiveAsync(Arg.Any<SocketAwaitable>()).Forget();

            _mockSocketListener.Received(2).TakeNewClient().Forget();

            _mockLogicalServer.Received(1).RegisterConnectedClient((IPEndPoint)socket.RemoteEndPoint, Arg.Any<ISocketReaderWriter>()).Forget();

            _mockLogicalServer.Received(1).ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Forget();

            _mockLogicalServer.DidNotReceive().ClientDiconnected(Arg.Any<IPEndPoint>()).Forget();
        }

        [Test]
        public async Task TestClientDisconnectionIfLogicalServerDecidedSo()
        {
            // set up
            var operationEndedTask = new TaskCompletionSource<int>();
            ISocket socket = Substitute.For<ISocket>();
            socket.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Any, 44412));
            socket.ReceiveAsync(Arg.Do<SocketAwaitable>(x => x.BytesTransferred = 5)).Returns(Task.FromResult(0));

            _mockSocketListener.TakeNewClient().Returns(Task.FromResult(socket), _taskCompletionSourceStopNotifier.Task);

            _mockLogicalServer.ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Returns(Task.FromResult(false)); // here logical server decided to break the connection

            _mockLogicalServer.ClientDiconnected(Arg.Do<IPEndPoint>(x => operationEndedTask.SetResult(0))).Forget();


            // execute
            var taskServerRun = _server.Start();

            // wait untill the test is over
            await operationEndedTask.Task;

            // check
            socket.Received(1).ReceiveAsync(Arg.Any<SocketAwaitable>()).Forget();

            _mockSocketListener.Received(2).TakeNewClient().Forget();

            _mockLogicalServer.Received(1).RegisterConnectedClient((IPEndPoint)socket.RemoteEndPoint, Arg.Any<ISocketReaderWriter>()).Forget();

            _mockLogicalServer.Received(1).ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Forget();

            _mockLogicalServer.Received(1).ClientDiconnected((IPEndPoint)socket.RemoteEndPoint).Forget();
        }

        [Test]
        public async Task TestClientDisconnectionIfThereWasAnIssueDuringRead()
        {
            //set up
            var operationEndedTask = new TaskCompletionSource<int>();

            ISocket socket = Substitute.For<ISocket>();
            socket.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Any, 44412));
            socket.ReceiveAsync(Arg.Do<SocketAwaitable>(x => x.BytesTransferred = 0)).Returns(Task.FromResult(0));

            _mockSocketListener.TakeNewClient().Returns(Task.FromResult(socket), _taskCompletionSourceStopNotifier.Task);

            _mockLogicalServer.ClientDiconnected(Arg.Do<IPEndPoint>(x => operationEndedTask.SetResult(0))).Forget();

            //execute
            var taskServerRun = _server.Start();

            await operationEndedTask.Task;

            //check
            socket.Received(1).ReceiveAsync(Arg.Any<SocketAwaitable>()).Forget();

            _mockSocketListener.Received(2).TakeNewClient().Forget();

            _mockLogicalServer.Received(1).RegisterConnectedClient(Arg.Any<IPEndPoint>(), Arg.Any<ISocketReaderWriter>()).Forget();

            _mockLogicalServer.DidNotReceive().ProcessRequestAsync(Arg.Any<ISocketReaderWriter>(), Arg.Any<Stream>()).Forget();

            _mockLogicalServer.Received(1).ClientDiconnected((IPEndPoint)socket.RemoteEndPoint).Forget();
        }
    }
}
