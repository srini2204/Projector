
namespace Projector.Data
{
    public interface IField<out T>
    {
        T GetValue();
    }
}
