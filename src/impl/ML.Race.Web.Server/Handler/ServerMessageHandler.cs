using MLL.Network.Factories;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using MLL.Race.Web.Common.Messages.Client;
using MLL.Race.Web.Common.Messages.Server;

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

public class ServerMessageHandler : MessageHandlerBusBase
{
    private readonly IMessageSender _messageSender;

    public ServerMessageHandler(IMessageSender messageSender)
    {
        AddHandler<PingMessage>(PingHandler);
        AddHandler<GameFrameMessage>(GameFrameHandler);
        _messageSender = messageSender;
    }

    private async ValueTask PingHandler(PingMessage ping)
    {
        await _messageSender.SendAsync(new PongMessage());
    }

    private void GameFrameHandler(GameFrameMessage gameFrame)
    {
    }
}
