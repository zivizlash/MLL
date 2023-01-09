using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
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
using MLL.Network.Factories;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using MLL.Race.Web.Common.Messages.Client;
using MLL.Race.Web.Common.Messages.Server;
using MLL.Race.Web.Server.Handler;
using MLL.ThreadingOptimization;
using System.Net;

namespace MLL.Race.Web.Server;

public class RaceNetFactory : RandomFillNetFactory
{
    public RaceNetFactory() : base(146) { }

    public override LayerComputerBuilderResult GetComputers(bool isForTrain) =>
        new LayerComputerBuilder(GetComputerFactory())
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .Build();

    public override LayerWeightsDefinition[] GetDefinitions() => 
        LayerWeightsDefinition.Builder
            .WithInputLayer(100, (240 * 320 * 3) + 1) // image pixel + value 1 const
            .WithLayer(2)
            .Build();

    private static ILayerComputerFactory GetComputerFactory() =>
        new BasicLayerComputerFactory(GetThreadingOptimizator());

    private static ThreadingOptimizatorFactory GetThreadingOptimizator() =>
        new ThreadingOptimizatorFactory(new(10000, 0.25f, Environment.ProcessorCount));
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

    public RaceNet(RaceNetFactory factory, Random random, float initialScore = float.MinValue)
    {
        var net = factory.Create(isForTrain: true);

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

public class RaceNetManager
{
    private readonly IMessageSender _messageSender;
    private readonly float[] _frameBuffer;

    private readonly NetLearningContext _learningContext;

    private MagickImage? _image;

    public RaceNetManager(IMessageSender messageSender, RaceNetFactory factory)
    {
        _messageSender = messageSender;
        _frameBuffer = new float[240 * 320 * 3 + 1];
        _frameBuffer[^1] = 1;
        _learningContext = new(factory);
    }

    private int _counter;

    private MagickImage GetImage(byte[] frameBytes)
    {
        if (_counter++ % 1000 == 0)
        {
            _image?.Dispose();
            return _image = new MagickImage(frameBytes);
        }

        if (_image == null)
        {
            return _image = new MagickImage(frameBytes);
        }
        else
        {
            _image.Read(frameBytes);
            return _image;
        }
    }

    public async Task RecognizeFrameAsync(GameFrameMessage gameFrame)
    {
        var image = GetImage(gameFrame.Frame);

        var pixels = image.GetPixelsUnsafe().ToByteArray(PixelMapping.RGB)
            ?? throw new InvalidOperationException();

        Check.LengthEqual(_frameBuffer.Length, image.Width * image.Height * 3 + 1, nameof(gameFrame));

        for (int i = 0; i < pixels.Length; i++)
        {
            _frameBuffer[i] = pixels[i] / 255.0f;
        }

        _frameBuffer[^1] = 1;

        var (forward, left) = _learningContext.Recognize(_frameBuffer);

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

        using var server = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(new ReflectionHandlerFactory<ServerMessageHandler>())
            .WithUsedTypes(typesProvider)
            .WithLoggerFactory(new LoggerFactory())
            .BuildServer();

        await server.WorkingTask;
    }
}
