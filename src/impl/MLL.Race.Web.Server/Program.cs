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
using System.Diagnostics.CodeAnalysis;
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

public struct WeightsOffsetStats
{
    private readonly Dictionary<int, float> _scores;
    private readonly int _resolution;
    private readonly float _step;

    public WeightsOffsetStats(int resolution)
    {
        _scores = new();
        _resolution = resolution;
        _step = 1.0f / resolution;
    }

    public void Add(int steps, float score)
    {
        _scores.Add(steps, score);
    }

    public float Get(int steps)
    {
        return _scores[steps];
    }
}

public struct WeightsOffsetContext
{
    public float TimesApplied { get; set; }
    public NetWeights Weights { get; set; }
    public NetWeights Offsets { get; set; }

    public WeightsOffsetContext(NetWeights weights, NetWeights offsets)
    {
        Weights = weights;
        Offsets = offsets;
        TimesApplied = 0;
    }

    public void Apply(float times)
    {
        var multiplier = times - TimesApplied;

        var weightsLayers = Weights.Layers;
        var offsetLayers = Offsets.Layers;

        for (int layerIndex = 0; layerIndex < weightsLayers.Length; layerIndex++)
        {
            var weightsLayer = weightsLayers[layerIndex].Weights;
            var offsetLayer = offsetLayers[layerIndex].Weights;

            for (int neuronIndex = 0; neuronIndex < weightsLayer.Length; neuronIndex++)
            {
                var weights = weightsLayer[neuronIndex];
                var offset = offsetLayer[neuronIndex];

                for (int weightIndex = 0; weightIndex < weights.Length; weightIndex++)
                {
                    weights[weightIndex] += offset[weightIndex] * multiplier;
                }
            }
        }

        TimesApplied = times;
    }
}

public readonly struct WeightsOffset
{
    public NetWeights Offsets { get; }

    public WeightsOffset(NetWeights offsets)
    {
        Offsets = offsets;
    }

    public WeightsOffsetContext CreateContext(NetWeights weights)
    {
        return new WeightsOffsetContext(weights, Offsets);
    }
}

public class WeightsRasterizer
{
    public WeightsOffset FindOffset(NetWeights from, NetWeights to, [NotNull] ref LayerWeights[]? offsetBuffer)
    {
        Check.LengthEqual(from.Layers.Length, to.Layers.Length, nameof(to));
        offsetBuffer ??= NetReplicator.CopyWeights(from.Layers);
        Check.LengthEqual(from.Layers.Length, offsetBuffer.Length, nameof(offsetBuffer));

        var fromLayers = from.Layers;
        var toLayers = to.Layers;

        for (int i = 0; i < from.Layers.Length; i++)
        {
            var fromLayer = fromLayers[i].Weights;
            var toLayer = toLayers[i].Weights;
            var offsetsLayer = offsetBuffer[i].Weights;

            for (int neuronIndex = 0; neuronIndex < fromLayer.Length; neuronIndex++)
            {
                var fromNeuron = fromLayer[neuronIndex];
                var toNeuron = toLayer[neuronIndex];
                var offset = offsetsLayer[neuronIndex]; 

                for (int weightIndex = 0; weightIndex < fromNeuron.Length; weightIndex++)
                {
                    offset[weightIndex] = toNeuron[weightIndex] - fromNeuron[weightIndex];
                }
            }
        }

        return new WeightsOffset(new(offsetBuffer));
    }
}

public class AdaptiveLearningRate
{
    private readonly int _newThreshold;
    private readonly int _oldThreshold;
    private readonly float _minimum;

    public const float IncreaseMult = 1.5f;
    public const float DecreaseMult = 0.95f;

    private int _newSelectCount = 0;
    private int _oldSelectCount = 0;

    public float LearningRate { get; private set; }

    public AdaptiveLearningRate(float initialLearningRate, 
        int newThreshold, int oldThreshold, float minimum)
    {
        LearningRate = initialLearningRate;
        _newThreshold = newThreshold;
        _oldThreshold = oldThreshold;
        _minimum = minimum;
    }

    public float SelectOldAndGet()
    {
        _newSelectCount = 0;

        if (++_oldSelectCount == _oldThreshold)
        {
            _oldSelectCount = 0;
            LearningRate = Math.Max(_minimum, LearningRate * DecreaseMult);
        }

        return LearningRate;
    }

    public float SelectNewAndGet()
    {
        _oldSelectCount = 0;

        if (++_newSelectCount == _newThreshold)
        {
            _newSelectCount = 0;
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

    private readonly AdaptiveLearningRate _learningRateContext;

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

    private MagickImage GetImage(byte[] frameBytes)
    {
        if (_image == null)
        {
            _image = new MagickImage(frameBytes);
        }
        else
        {
            _image.Read(frameBytes);
        }

        return _image;
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
