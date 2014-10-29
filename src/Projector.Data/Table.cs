
using System.Collections.Generic;
namespace Projector.Data
{
    public class Table : IDataProvider
    {
        private readonly ISchema _schema;

        private int _currentRowIndex = -1;

        private readonly List<int> _usedIndecies;

        private readonly List<int> _idsAdded;

        private readonly List<int> _idsDeleted;

        private readonly List<int> _idsUpdated;

        private IDataConsumer _consumer;

        public Table(ISchema schema)
        {
            _idsAdded = new List<int>();
            _idsDeleted = new List<int>();
            _idsUpdated = new List<int>();
            _usedIndecies = new List<int>();
            _schema = schema;
        }

        public void Set<T>(int rowIndex, string name, T value)
        {
            var writableField = _schema.GetWritableField<T>(rowIndex, name);
            writableField.SetValue(value);
        }

        public int NewRow()
        {
            _currentRowIndex++;
            _usedIndecies.Add(_currentRowIndex);
            _idsAdded.Add(_currentRowIndex);
            return _currentRowIndex;
        }

        public void RemoveRow(int rowIndex)
        {
            _usedIndecies.Remove(rowIndex);
            _idsDeleted.Add(rowIndex);
        }

        public void FireChanges()
        {
            var anyChanges = false;

            if (_idsAdded.Count > 0)
            {
                _consumer.OnAdd(_idsAdded.AsReadOnly());
                _idsAdded.Clear();
                anyChanges = true;
            }

            if (_idsUpdated.Count > 0)
            {
                _consumer.OnUpdate(_idsUpdated.AsReadOnly(), new List<IField>());
                _idsUpdated.Clear();
                anyChanges = true;
            }

            if (_idsDeleted.Count > 0)
            {
                _consumer.OnDelete(_idsDeleted.AsReadOnly());
                _idsDeleted.Clear();
                anyChanges = true;
            }

            if (anyChanges)
            {
                _consumer.OnSyncPoint();
            }
        }

        public void AddConsumer(IDataConsumer consumer)
        {
            _consumer = consumer;
            _consumer.OnSchema(_schema);

            if (_usedIndecies.Count > 0)
            {
                _consumer.OnAdd(_usedIndecies.AsReadOnly());
            }

            _consumer.OnSyncPoint();
        }


    }
}
