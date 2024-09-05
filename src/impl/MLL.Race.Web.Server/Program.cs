using BigGustave;
using Microsoft.Extensions.Logging;
using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Engines;
using MLL.Common.Factory;
using MLL.Common.Layer;
using MLL.Common.Optimization;
using MLL.Common.Tools;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
using MLL.Network.Builders;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using MLL.Race.Web.Common.Messages.Client;
using MLL.Race.Web.Common.Messages.Server;
using MLL.Race.Web.Server.Handler;
using MLL.ThreadingOptimization;
using NLog.Extensions.Logging;
using System.Net;

namespace MLL.Race.Web.Server;

public abstract class RaceFactoryBase : RandomFillerNetFactory
{
    private readonly NetSaver _netSaver;

    public RaceFactoryBase(NetSaver netSaver, int seed) : base(seed)
    {
        _netSaver = netSaver;
    }

    public override void PostCreation(ClassificationEngine net)
    {
        var savedInstance = _netSaver.Load();

        if (savedInstance != null)
        {
            net.Weights = new(savedInstance.Weights);
        }
        else
        {
            base.PostCreation(net);
        }
    }

    protected static ILayerComputerFactory GetComputerFactory() =>
        new BasicLayerComputerFactory(GetThreadingOptimizator());

    private static ThreadingOptimizatorFactory GetThreadingOptimizator() =>
        new ThreadingOptimizatorFactory(new(10000, 0.25f, Environment.ProcessorCount));
}

public class DistanceRanceNetFactory : RaceFactoryBase
{
    public DistanceRanceNetFactory(NetSaver netSaver) : base(netSaver, 146) { }

    public override LayerComputerBuilderResult GetComputers(bool isForTrain) =>
        new LayerComputerBuilder(GetComputerFactory())
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build(isForTrain);

    public override LayerWeightsDefinition[] GetDefinitions() =>
        LayerWeightsDefinition.Builder
            .WithInputLayer(20, 3)
            .WithLayer(20)
            .WithLayer(2)
            .Build();
}

public class ImageRaceNetFactory : RaceFactoryBase
{
    public ImageRaceNetFactory(NetSaver netSaver) : base(netSaver, 146) { }

    public override LayerComputerBuilderResult GetComputers(bool isForTrain) =>
        new LayerComputerBuilder(GetComputerFactory())
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build(isForTrain);

    public override LayerWeightsDefinition[] GetDefinitions() => 
        LayerWeightsDefinition.Builder
            .WithInputLayer(200, (240 * 320 * 3) + 1) // image pixels + value 1 const
            .WithLayer(300)
            .WithLayer(100)
            .WithLayer(2)
            .Build();
}

public class RaceNet
{
    public readonly LayerComputers[] LayerComputers;
    public readonly LayerWeights[] LayerWeights;
    public readonly OptimizationManager Optimizator;
    public readonly NetLayersBuffers Buffers;
    public readonly Random Random;

    public readonly PredictContext PredictContext;
    public ReinforcementTrainContext ReinforcementTrainContext;

    public float Score;

    public RaceNet(NetFactory factory, Random random, float initialScore = float.MinValue)
    {
        var net = factory.Create(forTrain: true);

        LayerComputers = net.Computers.ToArray();
        Optimizator = net.OptimizationManager;
        LayerWeights = net.Weights.Layers;
        Buffers = net.Buffers;
        Random = random;
        Score = initialScore;

        PredictContext = new PredictContext(new(LayerWeights), Buffers, LayerComputers);
        ReinforcementTrainContext = new ReinforcementTrainContext(
            new(LayerWeights), LayerComputers, 0.15f, Random);
    }

    public void UpdateLearningRate(float learningRate)
    {
        ReinforcementTrainContext = new ReinforcementTrainContext(
            new(LayerWeights), LayerComputers, learningRate, Random);
    }
}

public interface IRaceNetManager
{
    Task RecognizeFrameAsync(GameFrameMessage gameFrame);
    Task UpdateScoreAsync(GameResultMessage gameResult);
}

public interface IFrameMessageInputConverter
{
    float[] Convert(GameFrameMessage gameFrame);
}

public class ImageFrameMessageInputConverter : IFrameMessageInputConverter
{
    private readonly float[] _frameBuffer;

#pragma warning disable IDE1006 // Naming Styles
    private const int ImageWidth = 320;
    private const int ImageHeight = 240;
    private const int ImageChannels = 3;
    private const int ConstInputParamsCount = 1;
    private const int ImageInputParamsLength = ImageWidth * ImageHeight * ImageChannels;
    private const int InputArrayLength = ImageInputParamsLength + ConstInputParamsCount;
#pragma warning restore IDE1006 // Naming Styles

    public ImageFrameMessageInputConverter()
    {
        _frameBuffer = new float[InputArrayLength];
        _frameBuffer[^1] = 1;
    }

    public float[] Convert(GameFrameMessage gameFrame)
    {
        var image = Png.Open(gameFrame.Frame);

        Check.LengthEqual(_frameBuffer.Length, image.Width * image.Height * 3 + 1, nameof(gameFrame));

        int counter = 0;

        for (int width = 0; width < image.Width; width++)
        {
            for (int height = 0; height < image.Height; height++)
            {
                var pixel = image.GetPixel(width, height);

                _frameBuffer[counter + 0] = pixel.R / 255.0f;
                _frameBuffer[counter + 1] = pixel.G / 255.0f;
                _frameBuffer[counter + 2] = pixel.B / 255.0f;
                counter += 3;
            }
        }

        return _frameBuffer;
    }
}

public class DistanceFrameMessageInputConverter : IFrameMessageInputConverter
{
    private float[] _buffer;

    public DistanceFrameMessageInputConverter()
    {
        _buffer = new float[3];
        _buffer[^1] = 1;
    }

    public float[] Convert(GameFrameMessage msg)
    {
        var floatSize = sizeof(float);

        Check.LengthEqual(floatSize * 2, msg.Frame.Length, nameof(msg));

        _buffer[0] = BitConverter.ToSingle(msg.Frame, floatSize * 0);
        _buffer[1] = BitConverter.ToSingle(msg.Frame, floatSize * 1);

        return _buffer;
    }
}

public class RaceNetManager : IRaceNetManager
{
    private readonly IMessageSender _messageSender;
    private readonly IFrameMessageInputConverter _frameMessageInputConverter;
    private readonly NetLearningContext _learningContext;

    public RaceNetManager(IMessageSender messageSender, NetFactory factory, 
        NetSaver netSaver, IFrameMessageInputConverter frameMessageInputConverter)
    {
        _messageSender = messageSender;
        _frameMessageInputConverter = frameMessageInputConverter;
        _learningContext = new(factory, netSaver);
    }

    public async Task RecognizeFrameAsync(GameFrameMessage gameFrame)
    {
        var netInput = _frameMessageInputConverter.Convert(gameFrame);
        var (forward, left) = _learningContext.Recognize(netInput);

        await _messageSender.SendAsync(new CarMovementUpdateMessage 
        {
            Forward = forward, Left = left
        });
    }

    public Task UpdateScoreAsync(GameResultMessage gameResult)
    {
        _learningContext.UpdateScore(gameResult.Score);
        return Task.CompletedTask;
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        await ServeAsync();
    }

    private static async Task ServeAsync()
    {
        var typesProvider = new MessageTypesProvider(
            typeof(GameFrameMessage),
            typeof(GameResultMessage),
            typeof(TrackConfigurationUpdateMessage),
            typeof(CarMovementUpdateMessage));

        foreach (var type in typesProvider.GetTypes())
        {
            Console.WriteLine(type.FullName);
        }

        const bool imageBased = false;

        Console.WriteLine();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddNLog());
        var handlerFactory = new ServerHandlerFactory(imageBased);

        await using var server = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(handlerFactory)
            .WithUsedTypes(typesProvider)
            .WithLoggerFactory(loggerFactory)
            .BuildServer();

        await server.WorkingTask;
    }
}
