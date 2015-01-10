using Projector.Data;
using Projector.IO.Implementation.Protocol;
using System;
using System.Collections.Generic;
using System.IO;

namespace Projector.IO.Implementation.Server
{
    class NetworkAdapter : IDataConsumer
    {
        private ISchema _schema;

        private readonly Stream _stream;
        private readonly int _subscriptionId;

        public NetworkAdapter(Stream stream, int subscriptionId)
        {
            _stream = stream;
            _subscriptionId = subscriptionId;
        }

        public void OnAdd(IList<int> ids)
        {
            MessageComposer.WriteRowAddedMessage(_stream, _subscriptionId, ids, _schema);
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
            MessageComposer.WriteSchemaMessage(_stream, _subscriptionId, _schema);
        }

        public void OnSyncPoint()
        {
            MessageComposer.WriteSyncPointMessage(_stream, _subscriptionId);
        }
    }
}
