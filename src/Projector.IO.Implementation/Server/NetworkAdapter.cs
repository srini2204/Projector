using Projector.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace Projector.IO.Implementation.Server
{
    class NetworkAdapter : IDataConsumer
    {
        private ISchema _schema;

        private readonly Stream _stream;

        public NetworkAdapter(Stream stream)
        {
            _stream = stream;
        }

        public void OnAdd(IList<int> ids)
        {
            foreach (var id in ids)
            {
                foreach (var field in _schema.Columns)
                {
                    if (field.DataType == typeof(int))
                    {
                        var iField = _schema.GetField<int>(id, "");
                    }
                    else if (field.DataType == typeof(long))
                    {
                        var iField = _schema.GetField<long>(id, "");
                    }
                    else if (field.DataType == typeof(string))
                    {
                        var iField = _schema.GetField<string>(id, "");
                    }
                }
            }
        }

        public void OnUpdate(IList<int> ids, IList<IField> updatedFields)
        {
            throw new NotImplementedException();
        }

        public void OnDelete(IList<int> ids)
        {
            foreach (var id in ids)
            {

            }
        }

        public void OnSchema(ISchema schema)
        {
            _schema = schema;
        }

        public void OnSyncPoint()
        {
            throw new NotImplementedException();
        }
    }
}
