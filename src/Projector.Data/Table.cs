
using System;
using System.Collections.Generic;
namespace Projector.Data
{
    public class Table: IDataProvider
    {
        private readonly ISchema _schema;

        private int _currentRowIndex = -1;
        private bool _syncPointFired;

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
                _usedIndecies.AddRange(_idsAdded);
                FireOnAdd(_idsAdded.AsReadOnly());
                _idsAdded.Clear();
                anyChanges = true;
            }

            if (_idsUpdated.Count > 0)
            {
                FireOnUpdate(_idsUpdated.AsReadOnly(), new List<IField>());
                _idsUpdated.Clear();
                anyChanges = true;
            }

            if (_idsDeleted.Count > 0)
            {
                FireOnDelete(_idsDeleted.AsReadOnly());
                _idsDeleted.Clear();
                anyChanges = true;
            }

            if (anyChanges)
            {
                FireOnSyncPoint();
            }
        }

        public IDisconnectable AddConsumer(IDataConsumer consumer)
        {
            consumer.OnSchema(_schema);

            if (_usedIndecies.Count > 0)
            {
                consumer.OnAdd(_usedIndecies.AsReadOnly());
            }

            if (_syncPointFired)
            {
                consumer.OnSyncPoint();
            }

            OnAdd += consumer.OnAdd;
            OnDelete += consumer.OnDelete;
            OnUpdate += consumer.OnUpdate;
            OnSchema += consumer.OnSchema;
            OnSyncPoint += consumer.OnSyncPoint;

            return new Disconnectable(this, consumer);
        }

        public void RemoveConsumer(IDataConsumer consumer)
        {
            OnAdd -= consumer.OnAdd;
            OnDelete -= consumer.OnDelete;
            OnUpdate -= consumer.OnUpdate;
            OnSchema -= consumer.OnSchema;
            OnSyncPoint -= consumer.OnSyncPoint;
        }

        public ISchema Schema { get { return _schema; } }

        private event Action<IList<int>> OnAdd;
        private event Action<IList<int>, IList<IField>> OnUpdate;
        private event Action<IList<int>> OnDelete;
        private event Action<ISchema> OnSchema;
        private event Action OnSyncPoint;

        protected virtual void FireOnAdd(IList<int> ids)
        {
            var handler = OnAdd;
            if (handler != null)
            {
                handler(ids);
            }
        }

        protected virtual void FireOnDelete(IList<int> ids)
        {
            var handler = OnDelete;
            if (handler != null)
            {
                handler(ids);
            }
        }

        protected virtual void FireOnUpdate(IList<int> ids, IList<IField> fields)
        {
            var handler = OnUpdate;
            if (handler != null)
            {
                handler(ids, fields);
            }
        }

        protected virtual void FireOnSchema(ISchema schema)
        {
            var handler = OnSchema;
            if (handler != null)
            {
                handler(schema);
            }
        }

        protected virtual void FireOnSyncPoint()
        {
            _syncPointFired = true;
            var handler = OnSyncPoint;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
