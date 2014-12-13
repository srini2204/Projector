using System;
using System.Collections.Generic;

namespace Projector.Data
{
    public class Schema : ISchema
    {
        private readonly Dictionary<string, IField> _data;
        private readonly List<IField> _columnList;
        private readonly int _capacity;

        public Schema(int capacity)
        {
            _capacity = capacity;
            _data = new Dictionary<string, IField>();
            _columnList = new List<IField>();
        }

        public List<IField> Columns
        {
            get { return _columnList; }
        }


        public IField<T> GetField<T>(int id, string name)
        {
            IField field;
            if (_data.TryGetValue(name, out field))
            {
                field.SetCurrentRow(id);
                return (IField<T>)field;

            }

            throw new InvalidOperationException("Can't find column name: '" + name + "'");
        }

        public IWritableField<T> GetWritableField<T>(int id, string name)
        {
            IField field;
            if (_data.TryGetValue(name, out field))
            {
                field.SetCurrentRow(id);
                return (IWritableField<T>)field;

            }

            throw new InvalidOperationException("Can't find column name: '" + name + "'");
        }

        public void CreateField<T>(string name)
        {
            var field = new Field<T>(new List<T>(_capacity), name);
            _data.Add(name, field);
            _columnList.Add(field);
        }


    }
}
