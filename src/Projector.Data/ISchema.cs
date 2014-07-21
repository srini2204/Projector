using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projector.Data
{
    public interface ISchema
    {
        List<object> Columns { get; }

        IField GetField(long id, string name);
    }
}
