using NSubstitute;
using NUnit.Framework;
using Projector.IO.SocketHelpers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Projector.IO.Test.SocketHelpers
{
    [TestFixture]
    class SocketWrapperTest
    {
        private ObjectPool<SocketAwaitable> _socketEventArgsPool;
        private int _eventArgsBuferSize = 100;

        [SetUp]
        public void InitContext()
        {
            _socketEventArgsPool = new ObjectPool<SocketAwaitable>();

            var eventArgs = new SocketAsyncEventArgs();
            eventArgs.SetBuffer(new byte[_eventArgsBuferSize], 0, _eventArgsBuferSize);

            var theTempReceiveSendUserToken = new DataHoldingUserToken(eventArgs.Offset, 4);

            theTempReceiveSendUserToken.CreateNewDataHolder();

            eventArgs.UserToken = theTempReceiveSendUserToken;

            var soketAwaitable = new SocketAwaitable(eventArgs);

            _socketEventArgsPool.Push(soketAwaitable);

        }

        [Test]
        public async Task TestSendWhenDataIsSmallerThanBuferThreeIterration()
        {
            var iSocket = Substitute.For<ISocket>();
            var listBytes = new List<int>();
            iSocket.SendAsync(Arg.Any<SocketAwaitable>()).Returns(
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 20;
                listBytes.Add(aw.EventArgs.Count);
                return Task.FromResult(0);
            },
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 4;
                listBytes.Add(aw.EventArgs.Count);
                return Task.FromResult(0);
            },
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 5;
                listBytes.Add(aw.EventArgs.Count);
                return Task.FromResult(0);
            }
            );

            var socketWrapper = new SocketWrapper(_socketEventArgsPool, iSocket, 4, _eventArgsBuferSize);

            var res = await socketWrapper.SendAsync(PrepareData(25));

            Assert.True(res);
            iSocket.Received(3).SendAsync(Arg.Any<SocketAwaitable>());
            Assert.AreEqual(3, listBytes.Count);
            Assert.AreEqual(29, listBytes[0]);
            Assert.AreEqual(9, listBytes[1]);
            Assert.AreEqual(5, listBytes[2]);
        }

        [Test]
        public async Task TestSendWhenDataIsSmallerThanBuferOneIterration()
        {
            var iSocket = Substitute.For<ISocket>();
            var listBytes = new List<int>();
            iSocket.SendAsync(Arg.Any<SocketAwaitable>()).Returns(
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 29;
                listBytes.Add(aw.EventArgs.Count);
                return Task.FromResult(0);
            }
            );

            var socketWrapper = new SocketWrapper(_socketEventArgsPool, iSocket, 4, _eventArgsBuferSize);

            var res = await socketWrapper.SendAsync(PrepareData(25));

            Assert.True(res);
            iSocket.Received(1).SendAsync(Arg.Any<SocketAwaitable>());
            Assert.AreEqual(1, listBytes.Count);
            Assert.AreEqual(29, listBytes[0]);
        }

        [Test]
        public async Task TestSendWhenDataIsBiggerThanBuferThreeIterration()
        {
            var iSocket = Substitute.For<ISocket>();
            var listBytes = new List<int>();
            iSocket.SendAsync(Arg.Any<SocketAwaitable>()).Returns(
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 20;
                listBytes.Add(aw.EventArgs.Count);
                return Task.FromResult(0);
            },
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 40;
                listBytes.Add(aw.EventArgs.Count);
                return Task.FromResult(0);
            },
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 94;
                listBytes.Add(aw.EventArgs.Count);
                return Task.FromResult(0);
            }
            );

            var socketWrapper = new SocketWrapper(_socketEventArgsPool, iSocket, 4, _eventArgsBuferSize);

            var res = await socketWrapper.SendAsync(PrepareData(150));

            Assert.True(res);
            iSocket.Received(3).SendAsync(Arg.Any<SocketAwaitable>());
            Assert.AreEqual(3, listBytes.Count);
            Assert.AreEqual(100, listBytes[0]);
            Assert.AreEqual(100, listBytes[1]);
            Assert.AreEqual(94, listBytes[2]);
        }



        private static byte[] PrepareData(int length)
        {
            byte[] arrayOfBytesInPrefix = BitConverter.GetBytes(length);


            var resultArray = new byte[arrayOfBytesInPrefix.Length + length];

            Buffer.BlockCopy(arrayOfBytesInPrefix, 0, resultArray, 0, arrayOfBytesInPrefix.Length);

            return resultArray;
        }
    }
}
