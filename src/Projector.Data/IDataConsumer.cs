using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Projector.Data
{
    public interface IDataConsumer
    {
        void OnAdd(long[] ids, long count);
        void OnUpdate(long[] ids, long count);
        void OnDelete(long[] ids, long count);
        void OnSchema(ISchema schema);
        void OnSyncPoint();
    }
}
