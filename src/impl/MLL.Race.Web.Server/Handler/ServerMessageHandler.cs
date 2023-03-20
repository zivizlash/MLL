using MLL.Common.Factory;
using MLL.Network.Factories;
using MLL.Network.Message.Handlers;
using MLL.Race.Web.Common.Messages.Client;

namespace MLL.Race.Web.Server.Handler;

public class ServerHandlerFactory : IMessageHandlerFactory
{
    private readonly bool _imageBased;

    public Type MessageHandlerType => typeof(ServerMessageHandler);

    public ServerHandlerFactory(bool imageBased = false)
    {
        _imageBased = imageBased;   
    }

    public object CreateMessageHandler(MessageHandlerFactoryContext context)
    {
        var netSaver = new NetSaver(_imageBased ? "image_versions" : "distance_versions");

        NetFactory factory = _imageBased 
            ? new ImageRaceNetFactory(netSaver) 
            : new DistanceRanceNetFactory(netSaver);

        IFrameMessageInputConverter converter = _imageBased
            ? new ImageFrameMessageInputConverter()
            : new DistanceFrameMessageInputConverter();

        var manager = new RaceNetManager(context.MessageSender, factory, netSaver, converter);
        return new ServerMessageHandler(manager);
    }
}

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
