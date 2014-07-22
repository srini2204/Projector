using System.Collections.Generic;

namespace Projector.Data
{
    public interface ISchema
    {
        List<object> Columns { get; }

        IField<T> GetField<T>(int id, string name);
    }
}
