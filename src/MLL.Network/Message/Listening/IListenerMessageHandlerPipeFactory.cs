using MLL.Network.Message.Listening;

namespace MLL.Network.Message.Protocol;

public interface IListenerMessageHandlerPipeFactory
{
    ListenerMessageHandlerPipe Create(ListenerMessageHandlerPipeFactoryContext context);
}
