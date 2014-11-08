
using NUnit.Framework;
using Projector.IO.Implementation.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Test.Utils
{
    [TestFixture]
    class SyncLoopTest
    {
        private SyncLoop _syncLoop;
        [SetUp]
        public void InitContext()
        {
            _syncLoop = new SyncLoop();
        }

        [Test]
        public async Task TestSyncExecution()
        {
            var processTask = _syncLoop.StartProcessActions(CancellationToken.None);
            var done = false;
            await _syncLoop.Run(() =>
            {
                Thread.Sleep(1000);
                done = true;
            });

            Assert.IsTrue(done);
        }

        [Test]
        public async Task TestExceptionDuringExecution()
        {
            var processTask = _syncLoop.StartProcessActions(CancellationToken.None);
            var done = false;
            await _syncLoop.Run(() =>
            {
                throw new Exception();
            });

            Assert.IsTrue(done);
        }
    }
}
