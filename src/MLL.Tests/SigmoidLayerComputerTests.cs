using MLL.Common.Layer;
using MLL.Computers.Layers.Backpropagation;
using MLL.Computers.Layers.Sigmoid;
using MLL.Computers.Layers.Sum;
using NUnit.Framework;
using System;

namespace MLL.Tests;

[TestFixture]
public class SigmoidLayerComputerTests
{
    private readonly LayerComputers _layerComputer;
    private readonly LayerComputers _threadedLayerComputer;

    public SigmoidLayerComputerTests()
    {
        _layerComputer = new LayerComputers(
            new CommonErrorComputer { ThreadInfo = new(1) },
            new SigmoidPredictComputer { ThreadInfo = new(1) },
            new SigmoidCompensateComputer { ThreadInfo = new(1) }, 
            new ErrorBackpropagation());

        _threadedLayerComputer = new LayerComputers(
            new CommonErrorComputer { ThreadInfo = new(8) },
            new SigmoidPredictComputer { ThreadInfo = new(8) },
            new SigmoidCompensateComputer { ThreadInfo = new(8) },
            new ThreadedErrorBackpropagation {  ThreadInfo = new(1) });
    }

    [Test]
    public void SingleAndMultiThreaded_Predict_SameResults()
    {
        var random = new Random(676);
        var results = new float[10];
        var threadedResults = new float[10];

        var input = Helper.FillRandom(random, new float[100]);
        var weights = new LayerWeights(Helper.FillRandom(random, new float[10][], 100));

        _layerComputer.Predict.Predict(weights, input, results);
        _threadedLayerComputer.Predict.Predict(weights, input, threadedResults);

        CollectionAssert.AreEqual(results, threadedResults);
    }

    [Test]
    public void SingleAndMultiThreaded_Compensate_SameResults()
    {
        var random = new Random(676);
        const float learningRate = 0.001f;

        var errors = Helper.FillRandom(random, new float[10]);
        var input = Helper.FillRandom(random, new float[100]);
        var outputs = Helper.FillRandom(random, new float[10]);
        var weights = new LayerWeights(Helper.FillRandom(random, new float[10][], 100));
        var threadedWeights = new LayerWeights(Helper.Copy(weights.Weights));

        _layerComputer.Compensate.Compensate(weights, input, learningRate, errors, outputs);
        _layerComputer.Compensate.Compensate(threadedWeights, input, learningRate, errors, outputs);

        CollectionAssert.AreEqual(weights.Weights, threadedWeights.Weights);
    }

    [Test]
    public void SingleAndMultiThreaded_Error_SameResults()
    {
        var random = new Random(676);

        var errors = new float[10];
        var threadedErrors = new float[10];
        var expected = Helper.FillRandom(random, new float[10]);
        var outputs = Helper.FillRandom(random, new float[10]);

        _layerComputer.Error.CalculateErrors(outputs, expected, errors);
        _threadedLayerComputer.Error.CalculateErrors(outputs, expected, threadedErrors);

        CollectionAssert.AreEqual(errors, threadedErrors);
    }
}

public static class Helper
{
    public static float[] Copy(float[] src)
    {
        var result = new float[src.Length];
        Array.Copy(src, result, src.Length);
        return result;
    }

    public static float[][] Copy(float[][] src)
    {
        var result = new float[src.Length][];

        for (int i = 0; i < src.Length; i++)
        {
            Array.Copy(src[i], result[i] = new float[src[i].Length], src[i].Length);
        }

        return result;
    }

    public static float[][] FillRandom(Random random, float[][] buffer, int count = 0)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            FillRandom(random, buffer[i] ??= new float[count]);
        }

        return buffer;
    }

    public static float[] FillRandom(Random random, float[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = random.NextSingle() * 2f - 1.0f;
        }

        return buffer;
    }
}
