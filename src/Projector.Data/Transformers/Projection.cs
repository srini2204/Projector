using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projector.Data.Transformers
{
    public class Projection : IDataProvider, IDataConsumer
    {
        private ISchema _sourceSchema;
        private ISchema _resultSchema;

        public Projection()
        {

        }
        public void Subscribe(IDataConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public void OnAdd(long[] ids, long count)
        {
            for (long i = 0; i < count; i++)
            {
                var id = ids[i];
                
            }
        }

        public void OnUpdate(long[] ids, long count)
        {
            throw new NotImplementedException();
        }

        public void OnDelete(long[] ids, long count)
        {
            throw new NotImplementedException();
        }

        public void OnSchema(ISchema schema)
        {
            _sourceSchema = schema;
            _resultSchema = new ProjectionSchema(_sourceSchema);
        }

        public void OnSyncPoint()
        {
            throw new NotImplementedException();
        }
    }
}
