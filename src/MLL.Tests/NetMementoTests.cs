using MLL.Common.Layer;
using MLL.Common.Layer.Backpropagation;
using MLL.Common.Layer.Computers;
using MLL.Common.Engines;
using MLL.Common.Optimization;
using MLL.Common.Pooling;
using Moq;
using NUnit.Framework;
using System;

namespace MLL.Tests;

[TestFixture]
public class NetMementoTests
{
#pragma warning disable IDE1006 // Naming Styles
    private const int COUNT = 5;
#pragma warning restore IDE1006 // Naming Styles

    [Test]
    public void Forget()
    {
        var (net, memento, rnd, _) = Create();

        var state = memento.Capture(net);

        net.Weights = new NetWeights(CreateRandomWeights(rnd, COUNT));
        var expected = net.Weights.Layers;

        state.Forget();

        Assert.AreEqual(expected, net.Weights.Layers);
    }

    [Test]
    public void Apply()
    {
        var (net, memento, rnd, _) = Create();

        var state = memento.Capture(net);
        var expected = net.Weights.Layers;

        net.Weights = new NetWeights(CreateRandomWeights(rnd, COUNT));

        state.Apply();

        Assert.AreEqual(expected, net.Weights.Layers);
    }

    [Test]
    public void SeveralCapturesApply()
    {
        var (net, memento, rnd, _) = Create();

        var state1 = memento.Capture(net);
        var expectedState1 = net.Weights.Layers;

        net.Weights = new NetWeights(CreateRandomWeights(rnd, COUNT));

        var state2 = memento.Capture(net);
        var expectedState2 = net.Weights.Layers;

        state1.Apply();
        Assert.AreEqual(expectedState1, net.Weights.Layers);

        state2.Apply();
        Assert.AreEqual(expectedState2, net.Weights.Layers);
    }

    [Test]
    public void SeveralForgetOnlyInstance()
    {
        var (net, memento, _, _) = Create();

        var expected = net.Weights.Layers;
        var state = memento.Capture(net);

        state.Forget();

        Assert.Throws<ObjectDisposedException>(state.Forget);
    }

    [Test]
    public void SeveralApplyOnlyInstance()
    {
        var (net, memento, rnd, _) = Create();

        var state = memento.Capture(net);
        net.Weights = new NetWeights(CreateRandomWeights(rnd, COUNT));

        state.Apply();

        Assert.Throws<InvalidOperationException>(state.Apply);
    }

    private static (ClassificationEngine, NetMemento, Random, Pool<NetWeights>) Create()
    {
        var random = new Random();

        var computers = CreateMockComputers(COUNT);
        var weights = CreateRandomWeights(random, COUNT);
        var optManager = new OptimizationManager();
        var buffers = NetLayersBuffers.CreateByWeights(weights);

        var net = new ClassificationEngine(computers, weights, optManager, buffers);

        var pool = new Pool<NetWeights>(
            () => new NetWeights(CreateRandomWeights(random, COUNT)));

        var memento = new NetMemento(pool);

        return (net, memento, random, pool);
    }

    private static LayerWeights[] CreateRandomWeights(Random random, int count)
    {
        static void Fill(float[][] neurons, Random random, int count)
        {
            for (int i = 0; i < neurons.Length; i++)
            {
                var weights = neurons[i] = new float[count];

                for (int j = 0; j < count; j++)
                {
                    weights[j] = random.NextSingle();
                }
            }
        }

        var weights = new LayerWeights[count];

        for (int i = 0; i < count; i++)
        {
            float[][] neurons = new float[count][];
            Fill(neurons, random, count);
            weights[i] = new LayerWeights(neurons);
        }

        return weights;
    }

    private static LayerComputers[] CreateMockComputers(int count)
    {
        var computers = new LayerComputers[count];

        for (int i = 0; i < count; i++)
        {
            computers[i] = new LayerComputers(
                Mock.Of<IErrorComputer>(), Mock.Of<IPredictComputer>(),
                Mock.Of<ICompensateComputer>(), Mock.Of<IErrorBackpropagation>());
        }

        return computers;
    }
}
