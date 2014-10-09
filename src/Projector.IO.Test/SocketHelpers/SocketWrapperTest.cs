﻿using NSubstitute;
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

            Assert.AreEqual(1, _socketEventArgsPool.Count);
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

            Assert.AreEqual(1, _socketEventArgsPool.Count);
        }

        [Test]
        public async Task TestExceptionDuringSendOperation()
        {
            var iSocket = Substitute.For<ISocket>();

            iSocket.SendAsync(Arg.Any<SocketAwaitable>()).Returns(
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.EventArgs.SocketError = SocketError.OperationAborted;
                return Task.FromResult(0);
            }
            );

            var socketWrapper = new SocketWrapper(_socketEventArgsPool, iSocket, 4, _eventArgsBuferSize);

            var res = await socketWrapper.SendAsync(PrepareData(25));

            Assert.False(res);
            iSocket.Received(1).SendAsync(Arg.Any<SocketAwaitable>());
            iSocket.Received(1).Close();

            Assert.AreEqual(1, _socketEventArgsPool.Count);
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

            Assert.AreEqual(1, _socketEventArgsPool.Count);
        }

        [Test]
        public async Task TestReceiveWhenDataIsSmallerThanBuferOneIterration()
        {
            var iSocket = Substitute.For<ISocket>();

            iSocket.ReceiveAsync(Arg.Any<SocketAwaitable>()).Returns(
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 44;
                Buffer.BlockCopy(PrepareData(40), 0, aw.EventArgs.Buffer, 0, 44);

                return Task.FromResult(0);
            }
            );

            var socketWrapper = new SocketWrapper(_socketEventArgsPool, iSocket, 4, _eventArgsBuferSize);

            var data = await socketWrapper.ReceiveAsync();

            Assert.AreEqual(40, data.Length);
            iSocket.Received(1).ReceiveAsync(Arg.Any<SocketAwaitable>());


            Assert.AreEqual(1, _socketEventArgsPool.Count);
        }

        [Test]
        public async Task TestReceiveWhenDataIsSmallerThanBuferTwoIterration()
        {
            var iSocket = Substitute.For<ISocket>();
            var mockData = PrepareData(40);

            iSocket.ReceiveAsync(Arg.Any<SocketAwaitable>()).Returns(
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 24;
                Buffer.BlockCopy(mockData, 0, aw.EventArgs.Buffer, 0, 24);

                return Task.FromResult(0);
            },
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 20;
                Buffer.BlockCopy(mockData, 24, aw.EventArgs.Buffer, 0, 20);

                return Task.FromResult(0);
            }
            );

            var socketWrapper = new SocketWrapper(_socketEventArgsPool, iSocket, 4, _eventArgsBuferSize);

            var data = await socketWrapper.ReceiveAsync();

            Assert.AreEqual(40, data.Length);
            iSocket.Received(2).ReceiveAsync(Arg.Any<SocketAwaitable>());


            Assert.AreEqual(1, _socketEventArgsPool.Count);
        }

        [Test]
        public async Task TestReceiveWhenDataIsZeroLength()
        {
            var iSocket = Substitute.For<ISocket>();
            var mockData = PrepareData(0);

            iSocket.ReceiveAsync(Arg.Any<SocketAwaitable>()).Returns(
            x =>
            {
                var aw = x.Arg<SocketAwaitable>();
                aw.BytesTransferred = 4;
                Buffer.BlockCopy(mockData, 0, aw.EventArgs.Buffer, 0, 4);

                return Task.FromResult(0);
            }
            );

            var socketWrapper = new SocketWrapper(_socketEventArgsPool, iSocket, 4, _eventArgsBuferSize);

            var data = await socketWrapper.ReceiveAsync();

            Assert.AreEqual(0, data.Length);
            iSocket.Received(1).ReceiveAsync(Arg.Any<SocketAwaitable>());


            Assert.AreEqual(1, _socketEventArgsPool.Count);
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
