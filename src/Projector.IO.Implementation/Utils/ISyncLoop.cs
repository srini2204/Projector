using System;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Utils
{
    public interface ISyncLoop
    {
        Task Run(Action action);
    }
}
