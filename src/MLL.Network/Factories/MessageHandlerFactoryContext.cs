using MLL.Network.Message.Protocol;
using System;

namespace MLL.Network.Factories;

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
