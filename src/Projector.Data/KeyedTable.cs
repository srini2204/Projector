﻿using System.Collections.Generic;

namespace Projector.Data
{
    public class KeyedTable : IDataProvider
    {

        private readonly Dictionary<string, int> _keyToIdIndex = new Dictionary<string, int>();



        public KeyedTable(int capacity)
        {

        }




        public void AddConsumer(IDataConsumer consumer)
        {

        }






        IDisconnectable IDataProvider.AddConsumer(IDataConsumer consumer)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveConsumer(IDataConsumer consumer)
        {
            throw new System.NotImplementedException();
        }
    }
}
