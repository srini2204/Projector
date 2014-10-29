using System;
using System.Collections.Generic;

namespace Projector.Data
{
    public class Field<TData> : IField<TData>, IWritableField<TData>
    {
        private int _id;
        private List<TData> _data;

        public Field(List<TData> data)
        {
            _data = data;
        }

        public Type DataType
        {
            get { return typeof(TData); }
        }

        void IField.SetCurrentRow(int rowId)
        {
            _id = rowId;
        }

        public TData Value
        {
            get
            {
                return _data[_id];
            }
        }

        void IWritableField<TData>.SetValue(TData value)
        {
            _data[_id] = value;
        }
    }
}
