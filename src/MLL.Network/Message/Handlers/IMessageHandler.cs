using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers;

public interface IMessageHandler
{
    ValueTask HandleAsync(object message);
}

public interface IMessageHandler<TMessage> : IMessageHandler
{
    ValueTask HandleAsync(TMessage message);
}
