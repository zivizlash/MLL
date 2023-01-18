using Microsoft.Extensions.Logging;
using MLL.Common.Pooling;
using MLL.Network.Factories;
using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers.Binding;
using MLL.Network.Message.Protocol;
using System;

namespace MLL.Network.Message.Listening;

public class ListenerMessageHandlerPipeFactory : IListenerMessageHandlerPipeFactory
{
    private readonly MessageConverter _messageConverter;
    private readonly AttributeMessageHandlerBinder _handlerBinder;
    private readonly IMessageHandlerFactory _handlerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CollectionPool<byte> _dataPool;
    private readonly CollectionPool<byte> _internalPool;

    public ListenerMessageHandlerPipeFactory(MessageConverter messageConverter, 
        IMessageHandlerFactory handlerFactory, AttributeMessageHandlerBinder handlerBinder, 
        ILoggerFactory loggerFactory, CollectionPool<byte> dataPool, CollectionPool<byte> internalPool)
    {
        _messageConverter = messageConverter;
        _handlerFactory = handlerFactory;
        _handlerBinder = handlerBinder;
        _loggerFactory = loggerFactory;
        _dataPool = dataPool;
        _internalPool = internalPool;
    }

    public ListenerMessageHandlerPipe Create(ListenerMessageHandlerPipeFactoryContext context)
    {
        var clientInfo = context.ClientInfo;
        var connectionInfo = new TcpConnectionInfo(clientInfo.Client, clientInfo.Uid);

        var socketInfo = new SocketConnectionInfo(clientInfo.Uid, socket, TimeSpan.FromSeconds(5));

        var protocol = new MessageTcpProtocol(connectionInfo, _dataPool, _internalPool);
        var senderLogger = _loggerFactory.CreateLogger<MessageSender>();
        var sender = new MessageSender(protocol, _messageConverter, clientInfo.Uid, senderLogger);

        var factoryContext = new MessageHandlerFactoryContext(sender, context.ClientInfo.Uid);
        var handler = _handlerFactory.CreateMessageHandler(factoryContext);
        var bus = _handlerBinder.Bind(_handlerFactory.MessageHandlerType, handler);

        return new ListenerMessageHandlerPipe(bus, _messageConverter, protocol, clientInfo);
    }
}
