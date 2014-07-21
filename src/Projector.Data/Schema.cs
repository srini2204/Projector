using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projector.Data
{
    public class Schema : ISchema
    {
        private Dictionary<string, List<object>> _data;

        public Schema(Dictionary<string, List<object>> data)
        {
            _data = data;
        }
        public List<object> Columns
        {
            get { throw new NotImplementedException(); }
        }


        public IField GetField(long id, string name)
        {
            return null;
        }
    }
}
