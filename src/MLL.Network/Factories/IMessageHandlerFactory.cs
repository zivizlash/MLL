using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using System;
using System.Collections.Generic;

namespace MLL.Network.Factories;

public interface IMessageHandlerFactory
{
    IMultiMessageHandler CreateMessageHandler(MessageHandlerFactoryContext context);
    IEnumerable<Type> GetSendedTypes();
}

public readonly struct MessageHandlerFactoryContext
{
    public Guid Uid { get; }
    public IMessageSender MessageSender { get; }

    public MessageHandlerFactoryContext(IMessageSender messageSender, Guid uid)
    {
        MessageSender = messageSender;
        Uid = uid;
    }
}
