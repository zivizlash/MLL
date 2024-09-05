using System;

namespace MLL.Network.Factories;

public interface IMessageHandlerFactory
{
    object CreateMessageHandler(MessageHandlerFactoryContext context);
    Type MessageHandlerType { get; }
}
