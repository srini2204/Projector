
namespace Projector.Data.Filters
{
    public interface IFilterCriteria
    {
        bool Check(ISchema schema, long id);
    }
}
