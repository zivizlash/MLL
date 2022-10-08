using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Computers.Sum;
using MLL.Tools;

namespace MLL.Layer.Computers;

public interface IHasExecuteDelegate
{
    WaitCallback ExecuteDelegate { get; }
}

public static class ThreadTools
{
    public static void ExecuteOnThreadPool(IHasExecuteDelegate[] hasDelegates, int threadsCount)
    {
        if (threadsCount > 0)
        {
            for (int i = 0; i < hasDelegates.Length; i++)
            {
                ThreadPool.QueueUserWorkItem(hasDelegates[i].ExecuteDelegate);
            }
        }
    }

    public static (int start, int end) Loop(int itemsPerThread, int index)
    {
        int start = index * itemsPerThread;
        return (start, start + itemsPerThread);
    }

    public static int Counts(int threadsCount, int itemsCount)
    {
        if (threadsCount == 0)
        {
            return 0;
        }

        return itemsCount / threadsCount;
    }

    public static void EnsureCalculateWorkItems(ref LayerErrorCalcWorkItem[] workItems, float[] outputs, 
        float[] expected, float[] errors, ForkHelper fork)
    {
        if (fork.Countdown == null && fork.ThreadsCount > 0) throw new ArgumentNullException(nameof(fork.Countdown));
        if (workItems.Length != fork.ThreadsCount) workItems = new LayerErrorCalcWorkItem[fork.ThreadsCount];

        for (int itemIndex = 0; itemIndex < fork.ThreadsCount; itemIndex++)
        {
            var item = workItems[itemIndex] ??= new LayerErrorCalcWorkItem();
            item.Errors = errors;
            item.Outputs = outputs;
            item.Expected = expected;
            item.Index = itemIndex;
            item.Countdown = fork.Countdown!;
            item.ProcessingCount = fork.ProcessingCount;
        }
    }

    public static void EnsureCompensateWorkItems(ref SumLayerCompensateWorkItem[] workItems,
        LayerWeightsData layer, float[] input, float learningRate,
        float[] errors, float[] outputs, ForkHelper fork)
    {
        if (fork.Countdown == null && fork.ThreadsCount > 0) throw new ArgumentNullException(nameof(fork.Countdown));
        if (workItems.Length != fork.ThreadsCount) workItems = new SumLayerCompensateWorkItem[fork.ThreadsCount];

        for (int itemIndex = 0; itemIndex < fork.ThreadsCount; itemIndex++)
        {
            var item = workItems[itemIndex] ??= new SumLayerCompensateWorkItem();
            item.Index = itemIndex;
            item.Layer = layer;
            item.Input = input;
            item.LearningRate = learningRate;
            item.Errors = errors;
            item.Outputs = outputs;
            item.Countdown = fork.Countdown!;
            item.ProcessingCount = fork.ProcessingCount;
        }
    }

    public static void EnsureCompensateWorkItems(ref SigmoidLayerCompensateWorkItem[] workItems,
        LayerWeightsData layer, float[] input, float learningRate,
        float[] errors, float[] outputs, ForkHelper fork)
    {
        if (fork.Countdown == null && fork.ThreadsCount > 0) throw new ArgumentNullException(nameof(fork.Countdown));
        if (workItems.Length != fork.ThreadsCount) workItems = new SigmoidLayerCompensateWorkItem[fork.ThreadsCount];

        for (int itemIndex = 0; itemIndex < fork.ThreadsCount; itemIndex++)
        {
            var item = workItems[itemIndex] ??= new SigmoidLayerCompensateWorkItem();
            item.Index = itemIndex;
            item.Layer = layer;
            item.Input = input;
            item.LearningRate = learningRate;
            item.Errors = errors;
            item.Outputs = outputs;
            item.Countdown = fork.Countdown!;
            item.ProcessingCount = fork.ProcessingCount;
        }
    }

    public static void EnsurePredictWorkItems(ref SumLayerPredictWorkItem[] workItems,
        LayerWeightsData layer, float[] input, float[] results, ForkHelper fork)
    {
        int processingCount = fork.ProcessingCount;
        int count = fork.ThreadsCount;
        var countdown = fork.Countdown;

        if (countdown == null && count > 0) throw new ArgumentNullException(nameof(countdown));
        if (workItems.Length != count) workItems = new SumLayerPredictWorkItem[count];

        for (int itemIndex = 0; itemIndex < count; itemIndex++)
        {
            var item = workItems[itemIndex] ??= new SumLayerPredictWorkItem();
            item.Index = itemIndex;
            item.Layer = layer;
            item.Input = input;
            item.Results = results;
            item.Countdown = countdown!;
            item.ProcessingCount = processingCount;
        }
    }

    public static void EnsurePredictWorkItems(ref SigmoidLayerPredictWorkItem[] workItems,
        LayerWeightsData layer, float[] input, float[] results, ForkHelper fork)
    {
        int processingCount = fork.ProcessingCount;
        int count = fork.ThreadsCount;
        var countdown = fork.Countdown;

        if (countdown == null && count > 0) throw new ArgumentException(nameof(fork));
        if (workItems.Length != count) workItems = new SigmoidLayerPredictWorkItem[count];

        for (int itemIndex = 0; itemIndex < count; itemIndex++)
        {
            var item = workItems[itemIndex] ??= new SigmoidLayerPredictWorkItem();
            item.WorkItems = workItems;
            item.Index = itemIndex;
            item.Layer = layer;
            item.Input = input;
            item.Results = results;
            item.Countdown = countdown!;
            item.ProcessingCount = processingCount;
        }
    }
}

public class SumLayerPredictWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float[] Results;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

    public SumLayerPredictWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var neurons = Layer.Neurons;

        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        for (int ni = start; ni < end; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], Input);
            Results[ni] = sum;
        }

        Countdown.Signal();
    }
}

public class SigmoidLayerPredictWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float[] Results;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;
    public SigmoidLayerPredictWorkItem[] WorkItems;

    public WaitCallback ExecuteDelegate { get; }

    public SigmoidLayerPredictWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var neurons = Layer.Neurons;

        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        for (int ni = start; ni < end; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], Input);
            Results[ni] = NumberTools.Sigmoid(sum);
        }

        Countdown.Signal();
    }
}

public class LayerErrorCalcWorkItem : IHasExecuteDelegate
{
    public float[] Outputs;
    public float[] Expected;
    public float[] Errors;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

    public LayerErrorCalcWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        for (int neuronIndex = start; neuronIndex < end; neuronIndex++)
        {
            Errors[neuronIndex] = Expected[neuronIndex] - Outputs[neuronIndex];
        }

        Countdown.Signal();
    }
}

public class SumLayerCompensateWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float LearningRate;
    public float[] Errors;
    public float[] Outputs;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

    public SumLayerCompensateWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        var neurons = Layer.Neurons;

        for (int ni = start; ni < end; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(LearningRate, Errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * Input[wi];
            }
        }

        Countdown.Signal();
    }

    private static float GetGeneralError(float learningRate, float error)
    {
        return learningRate * error;
    }
}

public class SigmoidLayerCompensateWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float LearningRate;
    public float[] Errors;
    public float[] Outputs;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

    public SigmoidLayerCompensateWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        var neurons = Layer.Neurons;

        for (int ni = start; ni < end; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(LearningRate, Outputs[ni], Errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * Input[wi];
            }
        }

        Countdown.Signal();
    }

    private static float GetGeneralError(float learningRate, float output, float error)
    {
        float sigmoidDerivative = NumberTools.SigmoidDerivative(output);
        return learningRate * error * sigmoidDerivative;
    }
}
