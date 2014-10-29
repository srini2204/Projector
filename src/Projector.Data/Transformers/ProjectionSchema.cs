using System;
using System.Collections.Generic;

namespace Projector.Data.Transformers
{
    class ProjectionSchema : ISchema
    {
        public ProjectionSchema(ISchema sourceSchema)
        {

        }



        public List<IField> Columns
        {
            get { throw new NotImplementedException(); }
        }

        public IField<T> GetField<T>(int id, string name)
        {
            throw new NotImplementedException();
        }

        public IWritableField<T> GetWritableField<T>(int id, string name)
        {
            throw new NotImplementedException();
        }
    }
}
