using NUnit.Framework;
using Projector.IO.Implementation.Utils;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Test.Utils
{
    [TestFixture]
    class CircularStreamTest
    {
        [Test]
        public async Task TestReadAsync()
        {
            var circularStream = new CircularStream();
            var buffer = new byte[10];
            var bytesReadTask = circularStream.ReadAsync(buffer, 0, 10);

            Assert.IsFalse(bytesReadTask.IsCompleted);
            Assert.IsFalse(bytesReadTask.IsFaulted);

            var bufferWrite = new byte[5];

            circularStream.Write(bufferWrite, 0, 5);

            var bytesRead = await bytesReadTask;

            Assert.AreEqual(5, bytesRead);
        }

        [Test]
        public void TestWriteWithoutMemoryExtension()
        {
            var circularStream = new CircularStream(100);
            var buffer = GetBytesArray(10);
            circularStream.Write(buffer, 0, 10);

            Assert.AreEqual(10, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);
        }

        [Test]
        public void TestWriteWithMemoryExtensionUpTo256()
        {
            var circularStream = new CircularStream(10);
            var buffer = GetBytesArray(30);
            circularStream.Write(buffer, 0, 30);

            Assert.AreEqual(30, circularStream.Length);
            Assert.AreEqual(256, circularStream.Capacity);
        }

        [Test]
        public void TestWriteWithMemoryExtensionUpToBiggerSizes()
        {
            var circularStream = new CircularStream(10);
            var buffer = GetBytesArray(300);
            circularStream.Write(buffer, 0, 300);

            Assert.AreEqual(300, circularStream.Length);
            Assert.AreEqual(300, circularStream.Capacity);
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
            Assert.IsTrue(IsBytesArrayDataOk(readBuffer, 10), "Bytes we read seems to be not OK");
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
            Assert.IsTrue(IsBytesArrayDataOk(readBuffer, 10), "Bytes we read seems to be not OK");
            Assert.AreEqual(5, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);

            // here we will overlap

            circularStream.Write(writeBuffer, 15, 95);

            Assert.AreEqual(100, circularStream.Length);
            Assert.AreEqual(100, circularStream.Capacity);

            // read overlapped

            bytesRead = circularStream.Read(readBuffer, 10, 100);

            Assert.AreEqual(100, bytesRead);
            Assert.IsTrue(IsBytesArrayDataOk(readBuffer, 110), "Bytes we read seems to be not OK");
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
                Assert.IsTrue(IsBytesArrayDataOk(readBuffer, i * 16), "Bytes we read seems to be not OK");
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
                Assert.IsTrue(IsBytesArrayDataOk(readBuffer, i * 4), "Bytes we read seems to be not OK");
                Assert.AreEqual((i + 1) * 8 - (i + 1) * 4, circularStream.Length);

            }

            Assert.AreEqual(120, circularStream.Length);
            Assert.AreEqual(256, circularStream.Capacity);
        }

        [Test]
        public void TestOverlappingSeveralTimesWithExtensionMultiThreaded()
        {
            var circularStream = new CircularStream(10);
            var bytesToWrite = 800 * 1024;
            var bytesToRead = 600 * 1024;

            var writeBuffer = GetBytesArray(bytesToWrite);
            var readBuffer = new byte[bytesToRead];

            var taskWrite = Task.Run(() =>
            {
                var bytesWrittenTotal = 0;
                while (bytesWrittenTotal < bytesToWrite)
                {
                    circularStream.Write(writeBuffer, bytesWrittenTotal, 25);
                    bytesWrittenTotal += 25;
                }
            });

            var taskRead = Task.Run(() =>
            {
                var bytesReadTotal = 0;
                while (bytesReadTotal < bytesToRead)
                {
                    var bytesRead = circularStream.Read(readBuffer, bytesReadTotal, 4);

                    bytesReadTotal += bytesRead;
                }

            });

            taskWrite.Wait();
            taskRead.Wait();

            Assert.IsTrue(IsBytesArrayDataOk(readBuffer, bytesToRead), "Bytes we read seems to be not OK");
        }

        private static byte[] GetBytesArray(int count)
        {
            var byteArray = new byte[count];
            for (int i = 0; i < count; i++)
            {
                byteArray[i] = (byte)i;
            }

            return byteArray;
        }

        private static bool IsBytesArrayDataOk(byte[] byteArray, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (byteArray[i] != (byte)i)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
