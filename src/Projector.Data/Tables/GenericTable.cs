
namespace Projector.Data.Tables
{
    public class Table<T> : Table, IDataProvider<T>
    {
        public Table(IWritebleSchema schema)
            : base(schema)
        {

        }
    }
}
