using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projector.Data.Transformers
{
    class ProjectionSchema : ISchema
    {
        public ProjectionSchema(ISchema sourceSchema)
        {

        }

        public List<object> Columns
        {
            get { throw new NotImplementedException(); }
        }


        public IField GetField(long id, string name)
        {
            throw new NotImplementedException();
        }
    }
}
