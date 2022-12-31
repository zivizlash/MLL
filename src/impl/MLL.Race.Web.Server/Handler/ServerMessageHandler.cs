using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using MLL.Race.Web.Common.Messages.Client;

namespace MLL.Race.Web.Server.Handler;

public class ServerMessageHandler
{
    private readonly RaceNetManager _manager;

    public ServerMessageHandler(IMessageSender messageSender)
    {
        _manager = new RaceNetManager(messageSender, new RaceNetFactory());
    }

    [MessageHandler]
    public async Task TrackResult(GameResultMessage trackResult)
    {
        await _manager.UpdateScoreAsync(trackResult);
    }

    [MessageHandler]
    public async Task GameFrame(GameFrameMessage gameFrame)
    {
        await _manager.RecognizeFrameAsync(gameFrame);
    }
}
