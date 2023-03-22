using MLL.Network.Message.Handlers;
using MLL.Race.Web.Common.Messages.Client;

namespace MLL.Race.Web.Server.Handler;

public class ServerMessageHandler
{
    private readonly IRaceNetManager _manager;

    public ServerMessageHandler(IRaceNetManager manager) => _manager = manager;

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
