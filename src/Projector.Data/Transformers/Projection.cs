using System;

namespace Projector.Data.Transformers
{
    public class Projection : IDataProvider, IDataConsumer
    {
        private ISchema _sourceSchema;
        private ISchema _resultSchema;

        public Projection()
        {

        }
        public void AddConsumer(IDataConsumer consumer)
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

        public void OnAdd(System.Collections.Generic.IList<int> ids)
        {
            throw new NotImplementedException();
        }

        public void OnUpdate(System.Collections.Generic.IList<int> ids, System.Collections.Generic.IList<IField> updatedFields)
        {
            throw new NotImplementedException();
        }

        public void OnDelete(System.Collections.Generic.IList<int> ids)
        {
            throw new NotImplementedException();
        }
    }
}
