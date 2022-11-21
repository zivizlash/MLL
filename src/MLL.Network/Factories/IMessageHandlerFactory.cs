namespace MLL.Network.Factories;

public interface IMessageHandlerFactory
{
    object CreateMessageHandler(MessageHandlerFactoryContext context);
}
