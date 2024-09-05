using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public interface IMessageSender
{
    Task SendAsync<T>(T message, CancellationToken cancellationToken = default);
}
