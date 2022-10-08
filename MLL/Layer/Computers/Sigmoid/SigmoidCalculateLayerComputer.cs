using MLL.Layer.Backpropagation;
using MLL.Layer.Threading;
using MLL.Tools;

namespace MLL.Layer.Computers.Sigmoid;

public class NeuronComputers
{
    public ICalculateLayerComputer Calculate { get; set; }
    public IPredictLayerComputer Predict { get; set; }
    public ICompensateLayerComputer Compensate { get; set; }
    public IErrorBackpropagation ErrorBackpropagation { get; set; }
    
    public NeuronComputers(ICalculateLayerComputer calculate, IPredictLayerComputer predict, 
        ICompensateLayerComputer compensate, IErrorBackpropagation errorBackpropagation)
    {
        Calculate = calculate;
        Predict = predict;
        Compensate = compensate;
        ErrorBackpropagation = errorBackpropagation;
    }
}


public interface IThreadedComputer
{
    LayerThreadInfo ThreadInfo { get; set; }
}

public class ThreadedSumPredictLayerComputer : IPredictLayerComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SumLayerPredictWorkItem[] _workItems = Array.Empty<SumLayerPredictWorkItem>();

    public void Predict(LayerWeightsData layer, float[] input, float[] results)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        ThreadTools.EnsurePredictWorkItems(ref _workItems, layer, input, results, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(fork.ProcessingCount, fork.ThreadsCount);

        for (int ni = start; ni < neurons.Length; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], input);
            results[ni] = sum;
        }

        fork.Countdown?.Wait();
    }
}

public class ThreadedSumCompensateLayerComputer : ICompensateLayerComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SumLayerCompensateWorkItem[] _workItems = Array.Empty<SumLayerCompensateWorkItem>();

    public void Compensate(LayerWeightsData layer, float[] input, float learningRate, float[] errors, float[] outputs)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));
        Check.LengthEqual(neurons.Length, errors.Length, nameof(errors));
        Check.LengthEqual(neurons.Length, outputs.Length, nameof(outputs));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        ThreadTools.EnsureCompensateWorkItems(ref _workItems, layer, input, learningRate, errors, outputs, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(outputs.Length, fork.ThreadsCount);

        for (int ni = start; ni < neurons.Length; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(learningRate, errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * input[wi];
            }
        }

        fork.Countdown?.Wait();
    }

    private static float GetGeneralError(float learningRate, float error)
    {
        return learningRate * error;
    }
}

public class ThreadedSigmoidCompensateLayerComputer : ICompensateLayerComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SigmoidLayerCompensateWorkItem[] _workItems = Array.Empty<SigmoidLayerCompensateWorkItem>();

    public void Compensate(LayerWeightsData layer, float[] input, float learningRate, float[] errors, float[] outputs)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));
        Check.LengthEqual(neurons.Length, errors.Length, nameof(errors));
        Check.LengthEqual(neurons.Length, outputs.Length, nameof(outputs));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        ThreadTools.EnsureCompensateWorkItems(ref _workItems, layer, input, learningRate, errors, outputs, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(outputs.Length, fork.ThreadsCount);

        for (int ni = start; ni < neurons.Length; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(learningRate, outputs[ni], errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * input[wi];
            }
        }

        fork.Countdown?.Wait();
    }

    private static float GetGeneralError(float learningRate, float output, float error)
    {
        float sigmoidDerivative = NumberTools.SigmoidDerivative(output);
        return learningRate * error * sigmoidDerivative;
    }
}

public class ThreadedSigmoidPredictLayerComputer : IPredictLayerComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SigmoidLayerPredictWorkItem[] _workItems = Array.Empty<SigmoidLayerPredictWorkItem>();

    public void Predict(LayerWeightsData layer, float[] input, float[] results)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        ThreadTools.EnsurePredictWorkItems(ref _workItems, layer, input, results, fork);

        // Ошибка, Countdown=5, _workItems тоже 5 штук, но все равно меньше нуля
        // Переписать и проверить в лоб, что не так работает.

        //for (int threadIndex = 0; threadIndex < fork.ThreadsCount; threadIndex++)
        //{
        //    var workItem = _workItems[threadIndex];
        //    workItem.Execute(null);

        //    //ThreadPool.QueueUserWorkItem(workItem.Execute);
        //}

        ThreadTools.ExecuteOnThreadPool(_workItems, fork.ThreadsCount);
        
        var (start, _) = ThreadTools.Loop(fork.ProcessingCount, fork.ThreadsCount);

        for (int ni = start; ni < neurons.Length; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], input);
            results[ni] = NumberTools.Sigmoid(sum);
        }

        fork.Countdown?.Wait();
    }
}

public struct ForkHelper
{
    public CountdownEvent? Countdown;
    public int ThreadsCount;
    public int ProcessingCount;

    public static ForkHelper Create(LayerThreadInfo threadInfo, int neuronsCount)
    {
        int threadsCount = Math.Min(threadInfo.Threads - 1, neuronsCount);
        var countdown = threadsCount > 0 ? new CountdownEvent(threadsCount) : null;
        int processingCount = ThreadTools.Counts(threadInfo.Threads, neuronsCount);

        return new ForkHelper
        {
            ThreadsCount = threadsCount,
            ProcessingCount = processingCount,
            Countdown = countdown
        };
    }
}
