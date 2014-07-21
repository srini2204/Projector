using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Projector.Data
{
    public class Table<TRow> : IDataProvider
    {
        private Dictionary<string, List<object>> _data = new Dictionary<string, List<object>>();
        private Dictionary<string, long> _keyToIdIndex = new Dictionary<string, long>();

        private readonly Func<TRow, string> _keySelector;

        private PropertyInfo[] _rowProperties;


        private IDataConsumer _consumer;

        private long[] _addIds = new long[1024];
        private long _addIndex;

        private long[] _deleteIds = new long[1024];
        private long _deleteIndex;

        private ISchema _schema;

        public Table()
        {

        }

        public Table(Func<TRow, string> keySelector)
        {
            _keySelector = keySelector;
            _rowProperties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in _rowProperties)
            {
                _data.Add(property.Name, new List<object>());
            }

            _schema = new Schema(_data);
        }


        public void Subscribe(IDataConsumer consumer)
        {
            _consumer = consumer;
            CatchUp();
        }

        private void CatchUp()
        {
            _consumer.OnSchema(_schema);
            foreach (var id in _keyToIdIndex.Values)
            {
                _addIds[_addIndex] = id;
                _addIndex++;
            }

            if (_addIndex > 0)
            {
                _consumer.OnAdd(_addIds, _addIndex);
            }
            _consumer.OnSyncPoint();
            _addIndex = 0;
        }

        public long Add(TRow item)
        {
            long id = -1;
            var key = _keySelector(item);
            if (!_keyToIdIndex.TryGetValue(key, out id))
            {
                foreach (var property in _rowProperties)
                {
                    var columnList = _data[property.Name];
                    columnList.Add(property.GetValue(item));
                    id = columnList.Count - 1;
                }

                _addIds[_addIndex] = id;
                _addIndex++;
                _keyToIdIndex.Add(key, id);
            }

            return id;
        }
        public void Update(TRow item) { }
        public void Delete(TRow item)
        {
            long id = -1;
            var key = _keySelector(item);
            if (_keyToIdIndex.TryGetValue(key, out id))
            {
                _deleteIds[_deleteIndex] = id;
                _deleteIndex++;
                _keyToIdIndex.Remove(key);
            }
        }

        public void FireChanges()
        {
            if (_consumer != null)
            {
                _consumer.OnAdd(_addIds, _addIndex);
                _addIndex = 0;
                _consumer.OnDelete(_deleteIds, _deleteIndex);
                _deleteIndex = 0;
                _consumer.OnSyncPoint();
            }

            
            _addIndex = 0;
        }

    }
}
