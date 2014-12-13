using Projector.Data;
using Projector.IO.Implementation.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Projector.IO.Implementation.Protocol
{
    public static class MessageComposer
    {
        public static void WriteSyncPointMessage(Stream stream, int subscriptionId)
        {
            ByteBlader.WriteInt32(stream, 1);
            ByteBlader.WriteByte(stream, Constants.MessageType.SyncPoint);
        }

        public static void WriteOkMessage(Stream stream)
        {
            ByteBlader.WriteInt32(stream, 1);
            ByteBlader.WriteByte(stream, Constants.MessageType.Ok);
        }

        public static void WriteRowDeletedMessage(Stream stream, int subscriptionId, IList<int> ids)
        {
            var messageLength = ids.Count * 4 + 4 + 1;
            ByteBlader.WriteInt32(stream, messageLength);
            ByteBlader.WriteByte(stream, Constants.MessageType.RowDeleted);
            ByteBlader.WriteInt32(stream, subscriptionId);

            foreach (var id in ids)
            {
                ByteBlader.WriteInt32(stream, id);
            }
        }

        public static void WriteHeartbeatMessage(Stream stream)
        {
            ByteBlader.WriteInt32(stream, 0);
        }

        public static void WriteSchemaMessage(Stream stream, int subscriptionId, ISchema schema)
        {
            var memoryStream = new MemoryStream(1024);
            ByteBlader.WriteByte(memoryStream, Constants.MessageType.Schema);
            ByteBlader.WriteInt32(memoryStream, subscriptionId);

            foreach (var field in schema.Columns)
            {
                if (field.DataType == typeof(int))
                {
                    ByteBlader.WriteByte(memoryStream, Constants.FieldType.Int);
                }
                else if (field.DataType == typeof(long))
                {
                    ByteBlader.WriteByte(memoryStream, Constants.FieldType.Long);
                }
                else if (field.DataType == typeof(string))
                {
                    ByteBlader.WriteByte(memoryStream, Constants.FieldType.String);
                }

                byte[] arrayOfBytesColumnName = Encoding.ASCII.GetBytes(field.Name);
                ByteBlader.WriteInt32(memoryStream, arrayOfBytesColumnName.Length);
                ByteBlader.WriteBytes(memoryStream, arrayOfBytesColumnName);
            }

            var messageLength = (int)memoryStream.Position;
            memoryStream.Position = 0;

            ByteBlader.WriteInt32(stream, messageLength);
            memoryStream.CopyTo(stream);
        }
    }
}
