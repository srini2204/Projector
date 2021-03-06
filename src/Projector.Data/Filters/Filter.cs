﻿using System;

namespace Projector.Data.Filters
{
    public class Filter : IDataProvider, IDataConsumer
    {
        private readonly IFilterCriteria _filterCriteria;
        private IDataConsumer _consumer;
        private ISchema _schema;

        private readonly long[] _addIds = new long[1024];
        private long _addIndex;

        private readonly long[] _deleteIds = new long[1024];
        private long _deleteIndex;

        public Filter(IFilterCriteria filterCriteria)
        {
            _filterCriteria = filterCriteria;
        }

        public void AddConsumer(IDataConsumer consumer)
        {
            _consumer = consumer;
        }

        public void OnAdd(long[] ids, long count)
        {
            for (long i = 0; i < count; i++)
            {
                if (_filterCriteria.Check(_schema, ids[i]))
                {
                    _addIds[_addIndex] = ids[i];
                    _addIndex++;
                }
            }
        }



        public void OnDelete(long[] ids, long count)
        {
            for (long i = 0; i < count; i++)
            {
                if (_filterCriteria.Check(_schema, ids[i]))
                {
                    _deleteIds[_addIndex] = ids[i];
                    _deleteIndex++;
                }
            }
        }

        public void OnSchema(ISchema schema)
        {
            _schema = schema;
        }

        public void OnSyncPoint()
        {
            FireChanges();
        }

        private void FireChanges()
        {
            if (_consumer != null)
            {
                if (_addIndex > 0)
                {
                    //_consumer.OnAdd(_addIds, _addIndex);
                    _addIndex = 0;
                }

                if (_addIndex > 0)
                {
                    //_consumer.OnDelete(_deleteIds, _deleteIndex);
                    _deleteIndex = 0;
                }
                _consumer.OnSyncPoint();
            }

            _addIndex = 0;
            _deleteIndex = 0;
        }

        private void CatchUp()
        {
            _consumer.OnSchema(_schema);
            //foreach (var id in _keyToIdIndex.Values)
            //{
            //    _addIds[_addIndex] = id;
            //    _addIndex++;
            //}

            //if (_addIndex > 0)
            //{
            //    _consumer.OnAdd(_addIds, _addIndex);
            //}
            _consumer.OnSyncPoint();
            _addIndex = 0;
        }

        public void OnAdd(System.Collections.Generic.IList<int> ids)
        {
            throw new NotImplementedException();
        }

        public void OnUpdate(System.Collections.Generic.IList<int> ids, System.Collections.Generic.IList<IField> updatedFields)
        {
            throw new NotImplementedException();
        }

        public void OnDelete(System.Collections.Generic.IList<int> ids)
        {
            throw new NotImplementedException();
        }

        IDisconnectable IDataProvider.AddConsumer(IDataConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public void RemoveConsumer(IDataConsumer consumer)
        {
            throw new NotImplementedException();
        }
    }
}
