using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using MLL.Race.Web.Common.Messages.Client;
using MLL.Race.Web.Common.Messages.Server;

namespace ML.Race.Web.Server.Handler;

public class ServerMessageHandler
{
    private readonly IMessageSender _messageSender;

    public ServerMessageHandler(IMessageSender messageSender)
    {
        _messageSender = messageSender;
    }

    [MessageHandler]
    private async ValueTask PingHandler(PingMessage ping)
    {
        await _messageSender.SendAsync(new PongMessage());
    }

    [MessageHandler]
    private void GameFrameHandler(GameFrameMessage gameFrame)
    {
    }
}
