﻿using System;
using System.Collections.Generic;

namespace Projector.Data.Transformers
{
    class ProjectionSchema : ISchema
    {
        public ProjectionSchema(ISchema sourceSchema)
        {

        }

        public List<object> Columns
        {
            get { throw new NotImplementedException(); }
        }

        public IField<T> GetField<T>(int id, string name)
        {
            throw new NotImplementedException();
        }
    }
}
