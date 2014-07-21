using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projector.Data
{
    public interface IDataProvider
    {
        void Subscribe(IDataConsumer consumer);
    }
}
