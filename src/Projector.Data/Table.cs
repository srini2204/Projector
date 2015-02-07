﻿using System;
using System.Collections.Generic;

namespace Projector.Data
{
    public class Table : DataProviderBase
    {
        private readonly IWritebleSchema _schema;

        private readonly List<int> _idsUpdated;

        public Table(IWritebleSchema schema)
        {
            _idsUpdated = new List<int>();
            _schema = schema;
            SetSchema(_schema);
        }

        public void Set<T>(int rowIndex, string name, T value)
        {
            var writableField = _schema.GetWritableField<T>(rowIndex, name);
            writableField.SetValue(value);
        }

        public int NewRow()
        {
            var newRowId = _schema.GetNewRowId();
            AddId(newRowId);
            return newRowId;
        }

        public void RemoveRow(int rowIndex)
        {
            RemoveId(rowIndex);
        }

        public new void FireChanges()
        {
            base.FireChanges();
        }
    }
}
