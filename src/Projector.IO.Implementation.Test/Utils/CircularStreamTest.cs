using NUnit.Framework;
using Projector.IO.Implementation.Utils;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Test.Utils
{
    [TestFixture]
    class CircularStreamTest
    {
        [Test]
        public void TestWriteWithoutMemoryExtension()
        {
            var circularStream = new CircularStream(100);
            var buffer = GetBytesArray(10);
            circularStream.Write(buffer, 0, 10);

            Assert.AreEqual(10, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);

            var readBuffer = new byte[10];
            circularStream.Read(readBuffer, 0, 10);
            Assert.IsTrue(AreBytesArraysEqual(buffer, readBuffer, 10), "Bytes we read seems to be not OK");
        }

        [Test]
        public void TestWriteWithMemoryExtensionUpTo256()
        {
            var circularStream = new CircularStream(10);
            var buffer = GetBytesArray(30);
            circularStream.Write(buffer, 0, 30);

            Assert.AreEqual(30, circularStream.Length);
            Assert.AreEqual(256, circularStream.Capacity);

            var readBuffer = new byte[30];
            circularStream.Read(readBuffer, 0, 30);
            Assert.IsTrue(AreBytesArraysEqual(buffer, readBuffer, 30), "Bytes we read seems to be not OK");
        
        }

        [Test]
        public void TestWriteWithMemoryExtensionUpToBiggerSizes()
        {
            var circularStream = new CircularStream(10);
            var buffer = GetBytesArray(300);
            circularStream.Write(buffer, 0, 300);

            Assert.AreEqual(300, circularStream.Length);
            Assert.AreEqual(620, circularStream.Capacity);

            var readBuffer = new byte[300];
            circularStream.Read(readBuffer, 0, 300);
            Assert.IsTrue(AreBytesArraysEqual(buffer, readBuffer, 300), "Bytes we read seems to be not OK");
        }

        [Test]
        public void TestRead()
        {
            var circularStream = new CircularStream(100);
            var writeBuffer = GetBytesArray(15);
            circularStream.Write(writeBuffer, 0, 15);

            Assert.AreEqual(15, circularStream.Length);

            var readBuffer = new byte[10];
            var bytesRead = circularStream.Read(readBuffer, 0, 10);

            Assert.AreEqual(10, bytesRead);
            Assert.IsTrue(AreBytesArraysEqual(readBuffer, writeBuffer, 10), "Bytes we read seems to be not OK");
            Assert.AreEqual(5, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);
        }

        [Test]
        public void TestOverlapping()
        {
            var circularStream = new CircularStream(100);
            var writeBuffer = GetBytesArray(110);
            circularStream.Write(writeBuffer, 0, 15);

            Assert.AreEqual(15, circularStream.Length);

            var readBuffer = new byte[110];
            var bytesRead = circularStream.Read(readBuffer, 0, 10);

            Assert.AreEqual(10, bytesRead);
            Assert.IsTrue(AreBytesArraysEqual(readBuffer, writeBuffer, 10), "Bytes we read seems to be not OK");
            Assert.AreEqual(5, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);

            // here we will overlap

            circularStream.Write(writeBuffer, 15, 95);

            Assert.AreEqual(100, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);

            // read overlapped

            bytesRead = circularStream.Read(readBuffer, 10, 100);

            Assert.AreEqual(100, bytesRead);
            Assert.IsTrue(AreBytesArraysEqual(readBuffer, writeBuffer, 110), "Bytes we read seems to be not OK");
            Assert.AreEqual(0, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);
        }

        [Test]
        public void TestOverlappingSeveralTimes()
        {
            var circularStream = new CircularStream(10);
            var writeBuffer = GetBytesArray(800);
            var readBuffer = new byte[800];

            for (int i = 0; i < 50; i++)
            {
                circularStream.Write(writeBuffer, i * 16, 16);

                Assert.AreEqual(16, circularStream.Length);

                var bytesRead = circularStream.Read(readBuffer, i * 16, 16);

                Assert.AreEqual(16, bytesRead);
                Assert.IsTrue(AreBytesArraysEqual(readBuffer, writeBuffer, i * 16), "Bytes we read seems to be not OK");
                Assert.AreEqual(0, circularStream.Length);
                Assert.AreEqual(256, circularStream.Capacity);
            }
        }


        [Test]
        public void TestOverlappingSeveralTimesWithExtension()
        {
            var circularStream = new CircularStream(10);
            var writeBuffer = GetBytesArray(255);
            var readBuffer = new byte[255];

            for (int i = 0; i < 30; i++)
            {
                circularStream.Write(writeBuffer, i * 8, 8);

                var bytesRead = circularStream.Read(readBuffer, i * 4, 4);

                Assert.AreEqual(4, bytesRead);
                Assert.IsTrue(AreBytesArraysEqual(readBuffer, writeBuffer, i * 4), "Bytes we read seems to be not OK");
                Assert.AreEqual((i + 1) * 8 - (i + 1) * 4, circularStream.Length);

            }

            Assert.AreEqual(120, circularStream.Length);
            Assert.AreEqual(256, circularStream.Capacity);
        }

        [Test]
        public async Task TestOverlappingSeveralTimesWithExtensionMultiThreaded()
        {
            var circularStream = new CircularStream(10);
            var bytesToWrite = 80000 * 1024;
            var bytesToRead = 60000 * 1024;

            var writeBuffer = GetBytesArray(bytesToWrite);
            var readBuffer = new byte[bytesToRead];

            var taskWrite = Task.Run(async () =>
                {
                    var bytesWrittenTotal = 0;
                    while (bytesWrittenTotal < bytesToWrite)
                    {
                        await circularStream.WriteAsync(writeBuffer, bytesWrittenTotal, 25);
                        bytesWrittenTotal += 25;
                    }
                });

            var taskRead = Task.Run(async () =>
                {
                    var bytesReadTotal = 0;
                    while (bytesReadTotal < bytesToRead)
                    {
                        var bytesRead = await circularStream.ReadAsync(readBuffer, bytesReadTotal, 4);

                        bytesReadTotal += bytesRead;
                    }

                });

            await taskWrite;
            await taskRead;

            Assert.IsTrue(AreBytesArraysEqual(readBuffer, writeBuffer, bytesToRead), "Bytes we read seems to be not OK");
        }

        [Test]
        public async Task TestWaitForDataThenWrite()
        {
            var circularStream = new CircularStream(100);
            var bytesToWrite = 100;

            var writeBuffer = GetBytesArray(bytesToWrite);

            // wait for data when no data yet arrived
            var taskWait = circularStream.WaitForData();

            Assert.False(taskWait.IsCompleted);
            Assert.AreEqual(0, circularStream.Length);

            // write which has to notify
            circularStream.Write(writeBuffer, 0, 25);

            await taskWait;

            Assert.True(taskWait.IsCompleted);
            Assert.AreEqual(25, circularStream.Length);
        }

        [Test]
        public async Task TestWriteThenWaitForData()
        {
            var circularStream = new CircularStream(100);
            var bytesToWrite = 100;

            var writeBuffer = GetBytesArray(bytesToWrite);

            // write data while no one is waitng
            circularStream.Write(writeBuffer, 0, 25);

            var threadName = Thread.CurrentThread.Name;

            // wait for data when there are data inside
            await circularStream.WaitForData();

            Assert.AreEqual(threadName, Thread.CurrentThread.Name, "There shouldn't be rescheduling");
            Assert.AreEqual(25, circularStream.Length);
        }

        private static byte[] GetBytesArray(int count)
        {
            var byteArray = new byte[count];

            var r = new Random();

            for (int i = 0; i < count; i++)
            {
                byteArray[i] = (byte)r.Next(0, 255);
            }

            return byteArray;
        }

        private static bool AreBytesArraysEqual(byte[] sourceByteArray, byte[] destByteArray, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (sourceByteArray[i] != destByteArray[i])
                {
                    Debug.Print("Byte: " + i);
                    return false;
                }
            }

            return true;
        }
    }
}
