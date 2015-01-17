
namespace Projector.Data.Tables
{
    public class Table<T> : Table, IDataProvider<T>
    {
        public Table(ISchema schema)
            : base(schema)
        {

        }
    }
}
