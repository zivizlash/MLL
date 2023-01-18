using BigGustave;
using ImageMagick;
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
using MLL.Network.Factories;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using MLL.Race.Web.Common.Messages.Client;
using MLL.Race.Web.Common.Messages.Server;
using MLL.Race.Web.Server.Handler;
using MLL.ThreadingOptimization;
using NLog.Extensions.Logging;
using System.Net;

namespace MLL.Race.Web.Server;

public class RaceNetFactory : RandomFillNetFactory
{
    public RaceNetFactory() : base(146) { }

    public override LayerComputerBuilderResult GetComputers(bool isForTrain) =>
        new LayerComputerBuilder(GetComputerFactory())
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build();

    public override LayerWeightsDefinition[] GetDefinitions() => 
        LayerWeightsDefinition.Builder
            .WithInputLayer(100, (240 * 320 * 3) + 1) // image pixel + value 1 const
            .WithLayer(10)
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

#pragma warning disable IDE1006 // Naming Styles
    private const int ImageWidth = 320;
    private const int ImageHeight = 240;
    private const int ImageChannels = 3;
    private const int ConstInputParamsCount = 1;
    private const int ImageInputParamsLength = ImageWidth * ImageHeight * ImageChannels;
    private const int InputArrayLength = ImageInputParamsLength + ConstInputParamsCount;
#pragma warning restore IDE1006 // Naming Styles

    public RaceNetManager(IMessageSender messageSender, RaceNetFactory factory)
    {
        _messageSender = messageSender;
        _frameBuffer = new float[InputArrayLength];
        _frameBuffer[^1] = 1;
        _learningContext = new(factory);
    }

    private int _counter;

    public async Task RecognizeFrameAsync(GameFrameMessage gameFrame)
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

        Console.WriteLine();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddNLog());

        await using var server = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(new ReflectionHandlerFactory<ServerMessageHandler>())
            .WithUsedTypes(typesProvider)
            .WithLoggerFactory(loggerFactory)
            .BuildServer();

        await server.WorkingTask;
    }
}
