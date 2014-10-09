using NSubstitute;
using NUnit.Framework;
using Projector.IO.SocketHelpers;
using System;
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
        public async Task TestSendWhenDataIsSmallerThanBufer()
        {
            var mySocket = Substitute.For<MySocket>();
            mySocket.SendAsync(Arg.Any<SocketAsyncEventArgs>()).Returns(true);
            var socketWrapper = new SocketWrapper(_socketEventArgsPool, mySocket, 4, _eventArgsBuferSize);

            var res = await socketWrapper.SendAsync(PrepareData(25));

            Assert.True(res);
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
