using NUnit.Framework;
using Projector.IO.Implementation.Utils;
using System;
using System.IO;

namespace Projector.IO.Implementation.Test.Utils
{
    [TestFixture]
    class ByteBladerTest
    {
        [Test]
        public void TestWriteInt()
        {
            var stream = new MemoryStream();

            ByteBlader.WriteInt32(stream, int.MaxValue); // write with byteBlader

            Assert.AreEqual(4, stream.Position);

            stream.Position = 0;

            var bytes = stream.ToArray();

            var checkInt = BitConverter.ToInt32(bytes, 0); // read with something else

            Assert.AreEqual(int.MaxValue, checkInt);
        }

        [Test]
        public void TestReadInt()
        {
            var stream = new MemoryStream();

            var bytes = BitConverter.GetBytes(int.MaxValue); // write with something else

            stream.Write(bytes, 0, 4);

            stream.Position = 0;

            var checkInt = ByteBlader.ReadInt32(stream); // read with byteblader

            Assert.AreEqual(int.MaxValue, checkInt);
        }

        [Test]
        public void TestWriteLong()
        {
            var stream = new MemoryStream();

            ByteBlader.WriteLong(stream, long.MaxValue); // write with byteBlader

            Assert.AreEqual(8, stream.Position);

            stream.Position = 0;

            var bytes = stream.ToArray();

            var checkLong = BitConverter.ToInt64(bytes, 0); // read with something else

            Assert.AreEqual(long.MaxValue, checkLong);
        }

        [Test]
        public void TestReadLong()
        {
            var stream = new MemoryStream();

            var bytes = BitConverter.GetBytes(long.MinValue); // write with something else

            stream.Write(bytes, 0, 8);

            stream.Position = 0;

            var checkLong = ByteBlader.ReadLong(stream); // read with byteblader

            Assert.AreEqual(long.MinValue, checkLong);
        }

    }
}
