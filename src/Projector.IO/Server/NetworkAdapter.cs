using Projector.Data;
using Projector.IO.Protocol;
using System;
using System.Collections.Generic;
using System.IO;

namespace Projector.IO.Server
{
    class NetworkAdapter : IDataConsumer
    {
        private ISchema _schema;

        private readonly Stream _stream;
        private readonly int _subscriptionId;

        private readonly MessageComposer _messageComposer;

        public NetworkAdapter(Stream stream, int subscriptionId)
        {
            _stream = stream;
            _subscriptionId = subscriptionId;

            _messageComposer = new MessageComposer();
        }

        public void OnAdd(IList<int> ids)
        {
            _messageComposer.WriteRowAddedMessage(_stream, _subscriptionId, ids, _schema);
        }

        public void OnUpdate(IList<int> ids, IList<IField> updatedFields)
        {
            throw new NotImplementedException();
        }

        public void OnDelete(IList<int> ids)
        {
            MessageComposer.WriteRowDeletedMessage(_stream, _subscriptionId, ids);
        }

        public void OnSchema(ISchema schema)
        {
            _schema = schema;
            _messageComposer.WriteSchemaMessage(_stream, _subscriptionId, _schema);
        }

        public void OnSyncPoint()
        {
            MessageComposer.WriteSyncPointMessage(_stream, _subscriptionId);
        }
    }
}
