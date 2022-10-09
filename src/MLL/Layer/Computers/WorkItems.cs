using MLL.Common.Layer;
using MLL.Layer.Computers.Sigmoid;

namespace MLL.Layer.Computers;

public static class WorkItems
{
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
        LayerWeights layer, float[] input, float learningRate,
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
        LayerWeights layer, float[] input, float learningRate,
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
        LayerWeights layer, float[] input, float[] results, ForkHelper fork)
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
        LayerWeights layer, float[] input, float[] results, ForkHelper fork)
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
