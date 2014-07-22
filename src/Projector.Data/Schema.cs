using System;
using System.Collections.Generic;

namespace Projector.Data
{
    public class Schema : ISchema
    {
        private readonly Dictionary<string, List<object>> _data;

        public Schema(Dictionary<string, List<object>> data)
        {
            _data = data;
        }
        public List<object> Columns
        {
            get { throw new NotImplementedException(); }
        }


        public IField<T> GetField<T>(int id, string name)
        {
            List<object> column;
            if (_data.TryGetValue(name, out column))
            {
                return new Field<T>(column, id);

            }
            throw new InvalidOperationException("Can't find column name: '" + name + "'");
        }
    }
}
