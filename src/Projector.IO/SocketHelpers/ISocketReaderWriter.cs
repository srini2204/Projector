using System.IO;
using System.Threading.Tasks;

namespace Projector.IO.SocketHelpers
{
    public interface ISocketReaderWriter
    {
        Task DisconnectAsync();
        Task<bool> ReceiveAsync(Stream stream);
        Task<bool> SendAsync(Stream stream);
        object Token { get; set; }
    }
}
