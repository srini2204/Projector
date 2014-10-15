using NSubstitute;
using NUnit.Framework;
using Projector.IO.Protocol.CommandHandlers;
using Projector.IO.Server;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Projector.IO.Test.Server
{
    [TestFixture]
    class ServerTest
    {
        private Projector.IO.Server.Server _server;
        private ILogicalServer _mockLogicalServer;

        [SetUp]
        public void InitContext()
        {
            _mockLogicalServer = Substitute.For<ILogicalServer>();
            _server = new Projector.IO.Server.Server(new SocketListenerSettings(10, 1, 10, 4, 25, 10, new IPEndPoint(IPAddress.Any, 4441)), _mockLogicalServer);
        }

        [Test]
        public async Task TestStartStop()
        {
            var taskServerRun =_server.Start();

            Assert.False(taskServerRun.IsCompleted);

            _server.Stop();

            await taskServerRun;

            Assert.True(taskServerRun.IsCompleted);


        }


    }
}
