using MLL.Common.Layer;
using MLL.Common.Tools;
using MLL.Computers.Layers.Common.WorkInfo;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers;

public static class WorkItemsFiller
{
    private static void CheckCountdown(ForkJoinHelper fork)
    {
        if (fork.Countdown == null && fork.ThreadsCount > 1)
        {
            Throw.ArgumentNull(nameof(fork.Countdown));
        }
    }

    private static void EnsureItemsForEachThread<T>(ref T[] workItems, int threadsCount)
    {
        if (workItems.Length != threadsCount)
        {
            workItems = new T[threadsCount];
        }
    }

    public static void EnsureCalculateWorkItems<T>(ref T[] workItems, 
        float[] outputs, float[] expected, float[] errors, ForkJoinHelper fork)
        where T : IHasErrorWorkInfo, new()
    {
        CheckCountdown(fork);
        EnsureItemsForEachThread(ref workItems, fork.ThreadsCount);

        for (int itemIndex = 0; itemIndex < fork.ThreadsCount; itemIndex++)
        {
            var processingRange = fork.GetProcessingRange(itemIndex);

            var workInfo = new ErrorWorkInfo(outputs, expected, 
                errors, processingRange, fork.Countdown);

            (workItems[itemIndex] ??= new T()).WorkInfo = workInfo;
        }
    }

    public static void EnsureCompensateWorkItems<T>(ref T[] workItems,LayerWeights layer, 
        float[] input, float learningRate, float[] errors, float[] outputs, ForkJoinHelper fork) 
        where T : IHasCompensateWorkInfo, new()
    {
        CheckCountdown(fork);
        EnsureItemsForEachThread(ref workItems, fork.ThreadsCount);

        for (int itemIndex = 0; itemIndex < fork.ThreadsCount; itemIndex++)
        {
            var processingRange = fork.GetProcessingRange(itemIndex);

            var workInfo = new CompensateWorkInfo(layer, input, errors, 
                outputs, learningRate, processingRange, fork.Countdown);

            (workItems[itemIndex] ??= new T()).WorkInfo = workInfo;
        }
    } 

    public static void EnsurePredictWorkItems<T>(ref T[] workItems,
        LayerWeights layer, float[] input, float[] results, ForkJoinHelper fork)
        where T : IHasPredictWorkInfo, new()
    {
        CheckCountdown(fork);
        EnsureItemsForEachThread(ref workItems, fork.ThreadsCount);

        for (int itemIndex = 0; itemIndex < fork.ThreadsCount; itemIndex++)
        {
            var processingRange = fork.GetProcessingRange(itemIndex);

            var workInfo = new PredictWorkInfo(layer, input, 
                results, processingRange, fork.Countdown);

            (workItems[itemIndex] ??= new T()).WorkInfo = workInfo; // ToDo: reflection call
        }
    }
}
