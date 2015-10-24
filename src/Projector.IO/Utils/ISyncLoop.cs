using System;
using System.Threading.Tasks;

namespace Projector.IO.Utils
{
    public interface ISyncLoop
    {
        Task Run(Action action);
    }
}
