using System;
using System.IO;

namespace Projector.IO.Implementation.Utils
{
    public static class ByteBlader
    {
        public static int ReadInt32(Stream stream)
        {
            return (stream.ReadByte()
                    | ((stream.ReadByte()) << 8)
                    | ((stream.ReadByte()) << 16)
                    | (stream.ReadByte() << 24));
        }

        public static void WriteInt32(Stream stream, int value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }

        public static long ReadLong(Stream stream)
        {
            int lo = ReadInt32(stream);

            int hi = ReadInt32(stream);

            uint loUI = (uint)lo;
            long hiL = (long)hi;
            return loUI | (hiL << 32);
        }

        public static void WriteLong(Stream stream, long value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 32));
            stream.WriteByte((byte)(value >> 40));
            stream.WriteByte((byte)(value >> 48));
            stream.WriteByte((byte)(value >> 56));
        }

        public static string ReadString(Stream stream)
        {
            throw new NotImplementedException();
        }

        public static void WriteString(Stream stream, string value)
        {
            throw new NotImplementedException();
        }
    }
}
