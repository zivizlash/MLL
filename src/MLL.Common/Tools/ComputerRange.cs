using System.Collections;

namespace MLL.Common.Tools;

public enum SliceDistribution
{
    RemainderOnEnd,
    FillRemainderEvenly
}

public static class ProcessingRangeExtensions
{
    public static Memory<T> AsMemory<T>(this T[] arr, ProcessingRange range)
    {
        return arr.AsMemory(range.Start, range.Length);
    }

    public static Span<T> AsSpan<T>(this T[] arr, ProcessingRange range)
    {
        return arr.AsSpan(range.Start, range.Length);
    }
}

public readonly struct ProcessingRangeSliced : IEnumerable<ProcessingRange>
{
    public readonly int SlicesCount;
    public readonly int ItemsCount;
    public readonly int ItemsStart;
    public readonly int ItemsStop;
    public readonly SliceDistribution Distribution;

    private readonly int _sliceLength;
    private readonly int _sliceRemainder;

    public ProcessingRangeSliced(int start, int stop, int count, SliceDistribution distribution)
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

    public IEnumerator<ProcessingRange> GetEnumerator()
    {
        return Enumerable.Range(0, SlicesCount).Select(GetSlice).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Enumerable.Range(0, SlicesCount).Select(GetSlice).GetEnumerator();
    }
}

public readonly struct ProcessingRange : IEquatable<ProcessingRange>
{
    public readonly int Start;
    public readonly int Stop;

    public int Length => Stop - Start;

    public ProcessingRange(int start, int stop)
    {
        (Start, Stop) = (start, stop);
    }

    public static ProcessingRange FromLength(int start, int length)
    {
        return new ProcessingRange(start, start + length);
    }

    public static ProcessingRange From<T>(T[] arr)
    {
        return new ProcessingRange(0, arr.Length);
    }

    public ProcessingRangeSliced Slice(int count, SliceDistribution distribution)
    {
        return new ProcessingRangeSliced(Start, Stop, count, distribution);
    }

    public bool IsCanBeAppliedTo<T>(T[] arr)
    {
        return Start < arr.Length && Stop <= arr.Length;
    }

    public override string ToString()
    {
        return $"Start: {Start}; Stop: {Stop}";
    }

    public override bool Equals(object? obj)
    {
        return obj is ProcessingRange range && Equals(range);
    }

    public bool Equals(ProcessingRange other)
    {
        return Start == other.Start && Stop == other.Stop;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, Stop, Length);
    }

    public static bool operator ==(ProcessingRange left, ProcessingRange right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProcessingRange left, ProcessingRange right)
    {  
        return !(left == right);
    }

    public void Deconstruct(out int start, out int stop)
    {
        (start, stop) = (Start, Stop);
    }
}
