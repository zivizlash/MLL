using Microsoft.Extensions.Logging;
using MLL.Common.Pooling;
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

    private readonly ILoggerFactory _loggerFactory;

    public ListenerMessageHandlerPipeFactory(MessageConverter messageConverter, 
        IMessageHandlerFactory handlerFactory, AttributeMessageHandlerBinder handlerBinder, 
        ILoggerFactory loggerFactory)
    {
        _messageConverter = messageConverter;
        _handlerFactory = handlerFactory;
        _handlerBinder = handlerBinder;
        _loggerFactory = loggerFactory;
    }

    public ListenerMessageHandlerPipe Create(ListenerMessageHandlerPipeFactoryContext context)
    {
        var protocolLogger = _loggerFactory.CreateLogger<MessageTcpProtocol>();
        var connectionInfo = new TcpConnectionInfo(context.ClientInfo.Client, context.ClientInfo.Uid);
        var bytesPool = new CollectionPool<byte>(512);

        var protocol = new MessageTcpProtocol(connectionInfo, bytesPool, protocolLogger);
        var sender = new MessageSender(protocol, _messageConverter);

        var factoryContext = new MessageHandlerFactoryContext(sender, context.ClientInfo.Uid);
        var handler = _handlerFactory.CreateMessageHandler(factoryContext);

        var bus = _handlerBinder.Bind(_handlerFactory.MessageHandlerType, handler);
        return new ListenerMessageHandlerPipe(bus, _messageConverter, protocol);
    }
}
