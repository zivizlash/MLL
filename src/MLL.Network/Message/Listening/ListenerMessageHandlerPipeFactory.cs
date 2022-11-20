using MLL.Network.Factories;
using MLL.Network.Message.Converters;
using MLL.Network.Message.Protocol;

namespace MLL.Network.Message.Listening;

public class ListenerMessageHandlerPipeFactory : IListenerMessageHandlerPipeFactory
{
    private readonly MessageConverter _messageConverter;
    private readonly IMessageHandlerFactory _handlerFactory;

    public ListenerMessageHandlerPipeFactory(
        MessageConverter messageConverter, IMessageHandlerFactory handlerFactory)
    {
        _messageConverter = messageConverter;
        _handlerFactory = handlerFactory;
    }

    public ListenerMessageHandlerPipe Create(ListenerMessageHandlerPipeFactoryContext context)
    {
        var protocol = new MessageTcpProtocol(new(context.ClientInfo.Client));
        var sender = new MessageSender(protocol, _messageConverter);

        var factoryContext = new MessageHandlerFactoryContext(sender, context.ClientInfo.Uid);
        var handler = _handlerFactory.CreateMessageHandler(factoryContext);

        return new ListenerMessageHandlerPipe(handler, _messageConverter, protocol);
    }
}
