using System;
using System.Collections.Generic;

namespace Projector.Data.Filters
{
    public class Filter<T> : DataProviderBase, IDataProvider<T>, IDataConsumer
    {
        private Func<ISchema, int, bool> _filterCriteria;
        private ISchema _schema;
        private IDisconnectable _subscription;
        private IDataProvider<T> _sourceDataProvider;

        public Filter(IDataProvider<T> sourceDataProvider, Func<ISchema, int, bool> filter)
        {
            _filterCriteria = filter;
            _sourceDataProvider = sourceDataProvider;
            _subscription = sourceDataProvider.AddConsumer(this);
        }

        public void ChangeFilter(Func<ISchema, int, bool> filter)
        {
            _filterCriteria = filter;
            _subscription.Dispose();
            foreach (var id in UsedIds)
            {
                RemoveId(id);
            }
            _subscription = _sourceDataProvider.AddConsumer(this);
        }

        public void OnSchema(ISchema schema)
        {
            _schema = schema;
        }

        public void OnSyncPoint()
        {
            FireChanges();
        }

        public void OnAdd(IList<int> ids)
        {
            foreach (var id in ids)
            {
                if (_filterCriteria(_schema, id))
                {
                    AddId(id);
                }
            }
        }

        public void OnUpdate(IList<int> ids, IList<IField> updatedFields)
        {
            foreach (var id in ids)
            {
                if (UsedIds.Contains(id) && !_filterCriteria(_schema, id))
                {
                    RemoveId(id);
                }
                else if(!UsedIds.Contains(id) && _filterCriteria(_schema, id))
                {
                    AddId(id);
                }
            }
        }

        public void OnDelete(IList<int> ids)
        {
            foreach (var id in ids)
            {
                if (UsedIds.Contains(id))
                {
                    RemoveId(id);
                }
            }
        }
    }
}
