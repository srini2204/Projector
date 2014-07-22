using System.Collections.Generic;

namespace Projector.Data
{
    class Field<T> : IField<T>
    {
        private readonly List<object> _data;
        private readonly int _id;

        public Field(List<object> data, int id)
        {
            _data = data;
            _id = id;
        }

        public T GetValue()
        {
            return (T) _data[_id];
        }
    }
}
