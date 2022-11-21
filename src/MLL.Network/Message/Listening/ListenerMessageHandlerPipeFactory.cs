using MLL.Network.Factories;
using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers.Binding;
using MLL.Network.Message.Protocol;

namespace MLL.Network.Message.Listening;

public class ListenerMessageHandlerPipeFactory : IListenerMessageHandlerPipeFactory
{
    private readonly MessageConverter _messageConverter;
    private readonly AttributeMessageHandlerBinder _handlerBinder;
    private readonly IMessageHandlerFactory _handlerFactory;

    public ListenerMessageHandlerPipeFactory(MessageConverter messageConverter, 
        IMessageHandlerFactory handlerFactory, AttributeMessageHandlerBinder handlerBinder)
    {
        _messageConverter = messageConverter;
        _handlerFactory = handlerFactory;
        _handlerBinder = handlerBinder;
    }

    public ListenerMessageHandlerPipe Create(ListenerMessageHandlerPipeFactoryContext context)
    {
        var protocol = new MessageTcpProtocol(new(context.ClientInfo.Client));
        var sender = new MessageSender(protocol, _messageConverter);

        var factoryContext = new MessageHandlerFactoryContext(sender, context.ClientInfo.Uid);
        var handler = _handlerFactory.CreateMessageHandler(factoryContext);

        var bus = _handlerBinder.Bind(_handlerFactory.MessageHandlerType, handler);
        return new ListenerMessageHandlerPipe(bus, _messageConverter, protocol);
    }
}
