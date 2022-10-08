using MLL.Layer.Computers;
using System.Diagnostics;

namespace MLL.Layer.Threading.Adapters;

public interface ITimeTracker
{
    List<TimeSpan> Timings { get; }
}

public class CalculateLayerProcessorTimeTrackerDecorator : ICalculateLayerComputer, ITimeTracker
{
    public ICalculateLayerComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public CalculateLayerProcessorTimeTrackerDecorator(ICalculateLayerComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors)
    {
        var sw = Stopwatch.StartNew();
        Computer.CalculateErrors(outputs, expected, errors);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}

public class CompensateLayerProcessorTimeTrackerDecorator : ICompensateLayerComputer, ITimeTracker
{
    public ICompensateLayerComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public CompensateLayerProcessorTimeTrackerDecorator(ICompensateLayerComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void Compensate(LayerWeightsData layer, float[] input, float learningRate, float[] errors, float[] outputs)
    {
        var sw = Stopwatch.StartNew();
        Computer.Compensate(layer, input, learningRate, errors, outputs);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}

public class PredictLayerProcessorTimeTrackerDecorator : IPredictLayerComputer, ITimeTracker
{
    public IPredictLayerComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public PredictLayerProcessorTimeTrackerDecorator(IPredictLayerComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void Predict(LayerWeightsData layer, float[] input, float[] results)
    {
        var sw = Stopwatch.StartNew();
        Computer.Predict(layer, input, results);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
