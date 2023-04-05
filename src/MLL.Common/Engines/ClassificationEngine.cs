using MLL.Common.Layer;
using MLL.Common.Optimization;
using MLL.Common.Tools;

namespace MLL.Common.Engines;

public readonly struct PredictContext
{
    public readonly NetWeights Weights;
    public readonly NetLayersBuffers Buffers;
    public readonly LayerComputers[] Computers;

    public PredictContext(NetWeights weights, NetLayersBuffers buffers, LayerComputers[] computers)
    {
        Weights = weights;
        Buffers = buffers;
        Computers = computers;
    }
}

public readonly struct SupervisedTrainContext
{
    public readonly NetWeights Weights;
    public readonly NetLayersBuffers Buffers;
    public readonly LayerComputers[] Computers;
    public readonly float LearningRate;

    public SupervisedTrainContext(NetWeights weights, NetLayersBuffers buffers, 
        LayerComputers[] computers, float learningRate)
    {
        Weights = weights;
        Buffers = buffers;
        Computers = computers;
        LearningRate = learningRate;
    }
}

public readonly struct ThreadedRandomContext
{
    public readonly Random[] Randoms;

    public ThreadedRandomContext(Random[] randoms)
    {
        Randoms = randoms;
    }
}

public static class ThreadedRandomBuilder
{
    public static ThreadedRandomContext Create(int threads, int seed) =>
        CreateInternal(threads, new Random(seed));

    public static ThreadedRandomContext Create(int threads) =>
        CreateInternal(threads, new Random());

    private static ThreadedRandomContext CreateInternal(int threads, Random seedRandom)
    {
        var randoms = new Random[threads];

        for (int i = 0; i < threads; i++)
        {
            randoms[i] = new Random(seedRandom.Next());
        }

        return new ThreadedRandomContext(randoms);
    }
}

public readonly struct ReinforcementTrainContext
{
    public readonly NetWeights Weights;
    public readonly LayerComputers[] Computers;
    public readonly float LearningRate;
    public readonly Random Random;
   
    public ReinforcementTrainContext(NetWeights weights, LayerComputers[] computers, float learningRate, Random random)
    {
        Weights = weights;
        Computers = computers;
        LearningRate = learningRate;
        Random = random;
    }
}

public readonly struct ReinforcementSelectItemContext
{
    public readonly NetWeights Weights;
    public readonly float Score;
    public readonly bool IsReference;

    public ReinforcementSelectItemContext(NetWeights weights, float score, bool isReference)
    {
        Weights = weights;
        Score = score;
        IsReference = isReference;
    }
}

public readonly struct ReinforcementSelectContext
{
    public readonly ReinforcementSelectItemContext[] Items;

    public ReinforcementSelectContext(ReinforcementSelectItemContext[] items)
    {
        Items = items;
    }
}

public readonly struct ReinforcementSelectResult
{
    public readonly bool IsSelectedReference;
    public readonly ReinforcementSelectItemContext SelectedItem;

    public ReinforcementSelectResult(bool isSelectedReference, ReinforcementSelectItemContext selectedItem)
    {
        IsSelectedReference = isSelectedReference;
        SelectedItem = selectedItem;
    }
}

public static class PredictionCalculator
{
    public static float[] Predict(in PredictContext ctx, float[] input)
    {
        var layers = ctx.Weights.Layers;

        Check.LengthEqual(layers[0].Weights[0].Length, input.Length, nameof(input));
        var prediction = PredictInternal(ctx.Computers, ctx.Buffers, ctx.Weights, input);

        return prediction;
    }

    private static float[] PredictInternal(LayerComputers[] computers, 
        NetLayersBuffers layersBuffers, NetWeights netWeights, float[] input)
    {
        float[] layerInput = input;
        float[][] buffers = layersBuffers.Outputs;

        var layers = netWeights.Layers;

        for (int i = 0; i < computers.Length; i++)
        {
            var computer = computers[i];
            var weights = layers[i];
            float[] buffer = buffers[i];

            computer.Predict.Predict(weights, layerInput, buffer);
            layerInput = buffer;
        }

        float[] lastLayerOutput = layerInput;
        return lastLayerOutput;
    }
}

public class SupervisedTrainer
{
    public static ReadOnlySpan<float> Train(in SupervisedTrainContext ctx, float[] input, float[] expected)
    {
        var layers = ctx.Weights.Layers;
        var buffers = ctx.Buffers;

        Check.LengthEqual(layers[0].Weights[0].Length, input.Length, nameof(input));
        Check.LengthEqual(expected.Length, layers[^1].Weights.Length, nameof(expected));

        var predictionCtx = new PredictContext(ctx.Weights, buffers, ctx.Computers);
        PredictionCalculator.Predict(predictionCtx, input);

        var prediction = buffers.Outputs[^1];
        float[] outputErrors = CompensateOutputLayerError(ctx, input, prediction, expected);

        for (int bi = buffers.Outputs.Length - 2; bi >= 0; bi--)
        {
            float[] layerInput = bi == 0 ? input : buffers.Outputs[bi - 1];
            CompensateLayerError(ctx, layerInput, bi);
        }

        return outputErrors;
    }

    private static void CompensateLayerError(in SupervisedTrainContext ctx, float[] input, int layerIndex)
    {
        var buffers = ctx.Buffers;
        var computers = ctx.Computers;

        var layers = ctx.Weights.Layers;
        var layer = layers[layerIndex];

        var previousWeights = layers[layerIndex + 1];
        var output = buffers.Outputs[layerIndex];

        var previousErrors = buffers.Errors[layerIndex + 1];
        var errors = buffers.Errors[layerIndex];

        var compensate = computers[layerIndex].Compensate;
        var errorBackprop = computers[layerIndex + 1].ErrorBackpropagation;

        errorBackprop.ReorganizeErrors(new(previousWeights.Weights, previousErrors), errors);
        compensate.Compensate(layer, input, ctx.LearningRate, errors, output);
    }

    private static float[] CompensateOutputLayerError(
        in SupervisedTrainContext ctx, float[] input, float[] output, float[] expected)
    {
        var buffers = ctx.Buffers;

        float[] errorBuffer = buffers.Errors[^1];
        float[] previousOutput = buffers.Outputs.Length < 2 
            ? input 
            : buffers.Outputs[^2];

        var computer = ctx.Computers[^1];
        var layers = ctx.Weights.Layers;

        computer.Error.CalculateErrors(output, expected, errorBuffer);
        computer.Compensate.Compensate(layers[^1], previousOutput, ctx.LearningRate, errorBuffer, output);

        return errorBuffer;
    }
}

public class ReinforcementTrainer
{
    public static void Randomize(in ReinforcementTrainContext ctx, int layerIndex)
    {
        var layerWeights = ctx.Weights.Layers[layerIndex].Weights;

        for (int neuronIndex = 0; neuronIndex < layerWeights.Length; neuronIndex++)
        {
            var weights = layerWeights[neuronIndex];

            for (int weightIndex = 0; weightIndex < weights.Length; weightIndex++)
            {
                var delta = ctx.Random.Range(ctx.LearningRate);
                weights[weightIndex] += delta;
            }
        }
    }

    public static ReinforcementSelectResult SelectBest(ReinforcementSelectContext context)
    {
        var (index, _) = context.Items.FindMax(item => item.Score);
        var isSource = index == 0;
        return new ReinforcementSelectResult(isSource, context.Items[index]);
    }
}

public class ClassificationEngine
{
    private readonly LayerComputers[] _layersComputers;
    private readonly OptimizationManager _optimizationManager;
    private readonly NetLayersBuffers _buffers;

    private NetWeights _weights;

    public NetWeights Weights
    {
        get => _weights;
        set => _weights = value;
    }
    
    public ReadOnlySpan<LayerComputers> Computers => _layersComputers;
    public NetLayersBuffers Buffers => _buffers;
    public OptimizationManager OptimizationManager => _optimizationManager;

    public ClassificationEngine(IEnumerable<LayerComputers> layersComputers, IEnumerable<LayerWeights> layersWeights, 
        OptimizationManager optimizationManager, NetLayersBuffers buffers)
    {
        var computers = layersComputers.ToArray();
        var weights = layersWeights.ToArray();

        Check.LengthEqual(computers.Length, weights.Length, nameof(layersWeights));
        Check.BufferFit(buffers, weights, nameof(buffers));

        _buffers = buffers;
        _layersComputers = computers;
        _weights = new NetWeights(weights);
        _optimizationManager = optimizationManager;
    }

    public ReadOnlySpan<float> Train(float[] input, float[] expected, float learningRate)
    {
        var ctx = new SupervisedTrainContext(_weights, _buffers, _layersComputers, learningRate);
        var outputErrors = SupervisedTrainer.Train(ctx, input, expected);

        _optimizationManager.Optimize();
        return outputErrors;
    }

    public ReadOnlySpan<float> Predict(float[] input)
    {
        var ctx = new PredictContext(_weights, _buffers, _layersComputers);
        var result = PredictionCalculator.Predict(ctx, input);

        _optimizationManager.Optimize();
        return result;
    }
}
