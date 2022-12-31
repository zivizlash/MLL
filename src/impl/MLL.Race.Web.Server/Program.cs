using ImageMagick;
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
            .UseLayer<SumLayerDefine>()
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
        var net = factory.Create(true);

        LayerComputers = net.Computers.ToArray();
        Optimizator = net.OptimizationManager;
        LayerWeights = net.Weights.Layers;
        Buffers = net.Buffers;
        Random = random;

        PredictContext = new PredictContext(new(LayerWeights), Buffers, LayerComputers);
        ReinforcementTrainContext = new ReinforcementTrainContext(
            new(LayerWeights), LayerComputers, 0.15f, Random);

        Score = initialScore;
    }

    public void UpdateLearningRate(float learningRate)
    {
        ReinforcementTrainContext = new ReinforcementTrainContext(
            new(LayerWeights), LayerComputers, learningRate, Random);
    }
}

public class WeightsOffset
{

}

public class WeightsRasterizer
{
    //public WeightsOffset FindOffset(NetWeights src, NetWeights dst)
    //{
    //    NetReplicator.Copy
    //}
}

public class AdaptinveLearningRateContext
{
    private readonly int _newThreshold;
    private readonly int _oldThreshold;
    private readonly float _minimum;

    private const float IncreaseMult = 1.5f;
    private const float DecreaseMult = 0.95f;

    private int NewSelectCount = 0;
    private int OldSelectCount = 0;

    public float LearningRate { get; private set; }

    public AdaptinveLearningRateContext(float initialLearningRate, 
        int newThreshold, int oldThreshold, float minimum)
    {
        LearningRate = initialLearningRate;
        _newThreshold = newThreshold;
        _oldThreshold = oldThreshold;
        _minimum = minimum;
    }

    public float SelectOldAndGet()
    {
        NewSelectCount = 0;

        if (++OldSelectCount == _oldThreshold)
        {
            OldSelectCount = 0;
            LearningRate = Math.Max(_minimum, LearningRate * DecreaseMult);
        }

        return LearningRate;
    }

    public float SelectNewAndGet()
    {
        OldSelectCount = 0;

        if (++NewSelectCount == _newThreshold)
        {
            NewSelectCount = 0;
            LearningRate *= IncreaseMult;
        }

        return LearningRate;
    }
}

public class RaceNetManager
{
    private readonly IMessageSender _messageSender;
    private readonly RaceNet _net;
    private readonly RaceNet _referenceNet;
    private readonly float[] _frameBuffer;

    private readonly AdaptinveLearningRateContext _learningRateContext;

    private int _updatingLayer;

    private MagickImage? _image;

    public RaceNetManager(IMessageSender messageSender, RaceNetFactory factory)
    {
        _messageSender = messageSender;
        _net = new(factory, new Random());
        _referenceNet = new(factory, new Random());
        _frameBuffer = new float[240 * 320 * 3 + 1];
        _frameBuffer[^1] = 1;
        _updatingLayer = 0;
        _learningRateContext = new(0.25f, 5, 25, 0.05f);

        NetReplicator.CopyWeights(_referenceNet.LayerWeights, _net.LayerWeights);
    }

    private int _counter;

    public async Task RecognizeFrameAsync(GameFrameMessage gameFrame)
    {
        if (_image == null)
        {
            _image = new MagickImage(gameFrame.Frame);
        }
        else
        {
            _image.Read(gameFrame.Frame);
        }

        var pixels = _image.GetPixelsUnsafe().ToByteArray(PixelMapping.RGB)
            ?? throw new InvalidOperationException();

        Check.LengthEqual(_frameBuffer.Length, _image.Width * _image.Height * 3 + 1, nameof(_image));

        for (int i = 0; i < pixels.Length; i++)
        {
            _frameBuffer[i] = pixels[i] / 255.0f;
        }

        _frameBuffer[^1] = 1;

        var input = PredictionCalculator.Predict(_net.PredictContext, _frameBuffer);
        var (forward, left) = (input[0], input[1]);

        await _messageSender.SendAsync(new CarMovementUpdateMessage 
        {
            Forward = forward, Left = left
        });
    }

    public async Task UpdateScoreAsync(GameResultMessage gameResult)
    {
        _net.Score = gameResult.Score;

        RaceNet src, dst;

        float learningRate;

        if (_net.Score >= _referenceNet.Score)
        {
            (src, dst) = (_net, _referenceNet);
            learningRate = _learningRateContext.SelectNewAndGet();
            Console.WriteLine($"Updated net was selected; LearningRate: {_learningRateContext.LearningRate}");

            await _image!.WriteAsync($"C:\\Auto\\sended imgs\\{Interlocked.Increment(ref _counter)}.png");
        }
        else
        {
            (src, dst) = (_referenceNet, _net);
            Console.WriteLine($"Old net was selected; LearningRate: {_learningRateContext.LearningRate}");
            learningRate = _learningRateContext.SelectOldAndGet();
        }

        dst.Score = src.Score;
        NetReplicator.CopyLayer(new(src.LayerWeights), new(dst.LayerWeights), _updatingLayer);
        IncrementUpdatingLayer();

        _net.UpdateLearningRate(learningRate);
        ReinforcementTrainer.RandomizeWeights(_net.ReinforcementTrainContext, _updatingLayer);
    }

    private void IncrementUpdatingLayer()
    {
        _updatingLayer = (_updatingLayer + 1) % _referenceNet.LayerComputers.Length;
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        await ServeAsync();

        Console.WriteLine("Done!");
        Console.ReadLine();
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
            .BuildServer();

        await server.WorkingTask;
    }
}
