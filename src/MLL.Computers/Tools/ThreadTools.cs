using MLL.Common.Layer;
using MLL.Common.Tools;
using System.Collections;

namespace MLL.Computers.Tools;

public enum SliceDistribution
{
    RemainderOnEnd,
    FillRemainderEvenly
}

public readonly struct ComputerRangeSliced : IEnumerable<ProcessingRange>
{
    public readonly int SlicesCount;
    public readonly int ItemsCount;
    public readonly int ItemsStart;
    public readonly int ItemsStop;
    public readonly SliceDistribution Distribution;

    private readonly int _sliceLength;
    private readonly int _sliceRemainder;

    public ComputerRangeSliced(int start, int stop, int count, SliceDistribution distribution)
    {
        SlicesCount = count;
        ItemsStart = start;
        ItemsStop = stop;
        Distribution = distribution;

        ItemsCount = ItemsStop - ItemsStart;
        _sliceLength = ItemsCount / SlicesCount;
        _sliceRemainder = ItemsCount % SlicesCount;
    }

    public ProcessingRange GetSlice(int sliceIndex)
    {
        if (sliceIndex < 0 || sliceIndex >= SlicesCount)
        {
            Throw.ArgumentOutOfRange(nameof(sliceIndex));
        }

        switch (Distribution)
        {
            case SliceDistribution.RemainderOnEnd:
                {
                    var isEnd = sliceIndex == SlicesCount - 1;
                    var start = sliceIndex * _sliceLength + ItemsStart;
                    var stop = start + _sliceLength + (isEnd ? _sliceRemainder : 0);
                    return new ProcessingRange(start, stop);
                }

            case SliceDistribution.FillRemainderEvenly:
                {
                    var evenlyOffset = Math.Min(_sliceRemainder, sliceIndex);
                    var start = sliceIndex * _sliceLength + ItemsStart + evenlyOffset;
                    var stop = start + _sliceLength + (sliceIndex < _sliceRemainder ? 1 : 0);
                    return new ProcessingRange(start, stop);
                }

            default:
                Throw.InvalidOperation($"{nameof(Distribution)} has invalid state.");
                return new ProcessingRange();
        }
    }

    public IEnumerator<ProcessingRange> GetEnumerator() =>
        Enumerable.Range(0, SlicesCount).Select(GetSlice).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => 
        Enumerable.Range(0, SlicesCount).Select(GetSlice).GetEnumerator();
}

public readonly struct ProcessingRange : IEquatable<ProcessingRange>
{
    public readonly int Start;
    public readonly int Stop;

    public int Length => Stop - Start;

    public ProcessingRange(int start, int stop) =>
        (Start, Stop) = (start, stop);

    public static ProcessingRange FromLength(int start, int length) => 
        new(start, start + length);

    public static ProcessingRange From<T>(T[] arr) =>
        new(0, arr.Length);

    public ComputerRangeSliced Slice(int count, SliceDistribution distribution) =>
        new(Start, Stop, count, distribution);

    public override string ToString() =>
        $"Start: {Start}; Stop: {Stop}";

    public override bool Equals(object? obj) =>
        obj is ProcessingRange range && Equals(range);

    public bool Equals(ProcessingRange other) =>
        Start == other.Start && Stop == other.Stop;

    public override int GetHashCode() =>
        HashCode.Combine(Start, Stop, Length);

    public static bool operator ==(ProcessingRange left, ProcessingRange right) =>
        left.Equals(right);

    public static bool operator !=(ProcessingRange left, ProcessingRange right) =>
        !(left == right);

    public void Deconstruct(out int start, out int stop) =>
        (start, stop) = (Start, Stop);
}

public static class ThreadTools
{
    public static void ExecuteOnThreadPool(IHasExecuteDelegate[] works, CountdownEvent? countdown)
    {
        for (int i = 0; i < works.Length - 1; i++)
        {
            ThreadPool.QueueUserWorkItem(works[i].ExecuteDelegate, null, false);
        }

        works[^1].ExecuteDelegate.Invoke(null);
        countdown?.Wait();
    }

    public static ProcessingRange Loop(int itemsPerThread, int index)
    {
        int start = index * itemsPerThread;
        return new ProcessingRange(start, start + itemsPerThread);
    }

    public static int Counts(int threadsCount, int itemsCount)
    {
        if (threadsCount == 0)
        {
            return 0;
        }

        return itemsCount / threadsCount;
    }
}
