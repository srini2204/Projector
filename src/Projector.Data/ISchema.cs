using System.Collections.Generic;

namespace Projector.Data
{
    public interface ISchema
    {
        List<IField> Columns { get; }

        IField<T> GetField<T>(int id, string name);

        IWritableField<T> GetWritableField<T>(int id, string name);
    }
}
