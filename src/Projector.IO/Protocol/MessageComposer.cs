using Projector.Data;
using Projector.IO.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Projector.IO.Protocol
{
    public class MessageComposer
    {
        private MemoryStream _buffer;

        public MessageComposer()
        {
            _buffer = new MemoryStream(500 * 1024);
        }

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

        public void WriteSchemaMessage(Stream stream, int subscriptionId, ISchema schema)
        {
            ByteBlader.WriteInt32(_buffer, subscriptionId);
            ByteBlader.WriteByte(_buffer, Constants.MessageType.Schema);

            foreach (var field in schema.Columns)
            {
                if (field.DataType == typeof(int))
                {
                    ByteBlader.WriteByte(_buffer, Constants.FieldType.Int);
                }
                else if (field.DataType == typeof(long))
                {
                    ByteBlader.WriteByte(_buffer, Constants.FieldType.Long);
                }
                else if (field.DataType == typeof(string))
                {
                    ByteBlader.WriteByte(_buffer, Constants.FieldType.String);
                }

                byte[] arrayOfBytesColumnName = Encoding.ASCII.GetBytes(field.Name);
                ByteBlader.WriteInt32(_buffer, arrayOfBytesColumnName.Length);
                ByteBlader.WriteBytes(_buffer, arrayOfBytesColumnName);
            }

            var messageLength = (int)_buffer.Position;
            _buffer.Position = 0;

            ByteBlader.WriteInt32(stream, messageLength);
            _buffer.CopyTo(stream);
            _buffer.Position = 0;
        }

        public void WriteRowAddedMessage(Stream stream, int subscriptionId, IList<int> ids, ISchema schema)
        {
           foreach (var id in ids)
            {
                ByteBlader.WriteInt32(_buffer, subscriptionId);
                ByteBlader.WriteByte(_buffer, Constants.MessageType.RowAdded);

                var columndId = 0;
                foreach (var field in schema.Columns)
                {

                    ByteBlader.WriteInt32(_buffer, columndId);

                    if (field.DataType == typeof(int))
                    {
                        var iField = schema.GetField<int>(id, field.Name);
                        ByteBlader.WriteInt32(_buffer, iField.Value);
                    }
                    else if (field.DataType == typeof(long))
                    {
                        var iField = schema.GetField<long>(id, field.Name);
                        ByteBlader.WriteLong(_buffer, iField.Value);
                    }
                    else if (field.DataType == typeof(string))
                    {
                        var iField = schema.GetField<string>(id, field.Name);
                        byte[] arrayOfBytesStringValue = Encoding.ASCII.GetBytes(iField.Value);
                        ByteBlader.WriteInt32(_buffer, arrayOfBytesStringValue.Length);
                        ByteBlader.WriteBytes(_buffer, arrayOfBytesStringValue);
                    }

                    columndId++;
                }

                var messageLength = (int)_buffer.Position;
                _buffer.Position = 0;

                ByteBlader.WriteInt32(stream, messageLength);
                _buffer.CopyTo(stream);

                _buffer.Position = 0;
            }
        }
    }
}
