using MLL.Network.Factories;
using MLL.Network.Message.Handlers;

namespace ML.Race.Web.Server.Handler;

public class ServerMessageHandlerFactory : IMessageHandlerFactory
{
    public IMultiMessageHandler CreateMessageHandler(MessageHandlerFactoryContext context)
    {
        return new ServerMessageHandler(context.MessageSender);
    }

    public IEnumerable<Type> GetSendedTypes()
    {
        throw new NotImplementedException();
    }
}
