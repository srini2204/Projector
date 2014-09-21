
namespace Projector.Data
{
    public interface IDataProvider
    {
        void Subscribe(IDataConsumer consumer);
    }
}
