using Projector.Data;
using Projector.IO.Implementation.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Projector.IO.Implementation.Protocol
{
    public static class MessageComposer
    {
        public static void WriteSubscribeMessage(Stream stream, int subscriptionId, string tableName)
        {
            byte[] arrayOfBytesInMessage = Encoding.ASCII.GetBytes(tableName);
            ByteBlader.WriteInt32(stream, 5 + arrayOfBytesInMessage.Length);
            ByteBlader.WriteInt32(stream, subscriptionId);
            ByteBlader.WriteByte(stream, Constants.MessageType.Subscribe);
            ByteBlader.WriteBytes(stream, arrayOfBytesInMessage);

        }

        public static void WriteSyncPointMessage(Stream stream, int subscriptionId)
        {
            ByteBlader.WriteInt32(stream, 5);
            ByteBlader.WriteInt32(stream, subscriptionId);
            ByteBlader.WriteByte(stream, Constants.MessageType.SyncPoint);
        }

        public static void WriteOkMessage(Stream stream, int subscriptionId)
        {
            ByteBlader.WriteInt32(stream, 5);
            ByteBlader.WriteInt32(stream, subscriptionId);
            ByteBlader.WriteByte(stream, Constants.MessageType.Ok);
        }

        public static void WriteRowDeletedMessage(Stream stream, int subscriptionId, IList<int> ids)
        {
            var messageLength = ids.Count * 4 + 4 + 1;
            ByteBlader.WriteInt32(stream, messageLength);
            ByteBlader.WriteInt32(stream, subscriptionId);
            ByteBlader.WriteByte(stream, Constants.MessageType.RowDeleted);

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
            ByteBlader.WriteInt32(memoryStream, subscriptionId);
            ByteBlader.WriteByte(memoryStream, Constants.MessageType.Schema);

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

        public static void WriteRowAddedMessage(Stream stream, int subscriptionId, IList<int> ids, ISchema _schema)
        {
            var memoryStream = new MemoryStream(1024);
            foreach (var id in ids)
            {
                ByteBlader.WriteInt32(memoryStream, subscriptionId);
                ByteBlader.WriteByte(memoryStream, Constants.MessageType.RowAdded);

                var columndId = 0;
                foreach (var field in _schema.Columns)
                {

                    ByteBlader.WriteInt32(memoryStream, columndId);

                    if (field.DataType == typeof(int))
                    {
                        var iField = _schema.GetField<int>(id, field.Name);
                        ByteBlader.WriteInt32(memoryStream, iField.Value);
                    }
                    else if (field.DataType == typeof(long))
                    {
                        var iField = _schema.GetField<long>(id, field.Name);
                        ByteBlader.WriteLong(memoryStream, iField.Value);
                    }
                    else if (field.DataType == typeof(string))
                    {
                        var iField = _schema.GetField<string>(id, field.Name);
                        byte[] arrayOfBytesStringValue = Encoding.ASCII.GetBytes(iField.Value);
                        ByteBlader.WriteInt32(memoryStream, arrayOfBytesStringValue.Length);
                        ByteBlader.WriteBytes(memoryStream, arrayOfBytesStringValue);
                    }

                    columndId++;
                }

                var messageLength = (int)memoryStream.Position;
                memoryStream.Position = 0;

                ByteBlader.WriteInt32(stream, messageLength);
                memoryStream.CopyTo(stream);

                memoryStream.SetLength(0);
            }
        }
    }
}
