using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using MLL.Race.Web.Common.Messages.Client;

namespace ML.Race.Web.Server.Handler;

public class ServerMessageHandler
{
    private readonly IMessageSender _messageSender;

    public ServerMessageHandler(IMessageSender messageSender)
    {
        _messageSender = messageSender;
    }

    [MessageHandler]
    public void TrackResult(TrackResultMessage trackResult)
    {
    }

    [MessageHandler]
    public void GameFrame(GameFrameMessage gameFrame)
    {
    }
}
