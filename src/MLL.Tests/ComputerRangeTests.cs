using MLL.Computers.Tools;
using NUnit.Framework;
using System.Linq;

namespace MLL.Tests;

[TestFixture]
public class ComputerRangeTests
{
    [Test]
    public void RemainderOnEnd_Valid()
    {
        var source = Enumerable.Range(0, 14).ToArray();

        var slices = ProcessingRange.From(source).Slice(4, SliceDistribution.RemainderOnEnd);

        var expected = new ProcessingRange[]
        {
            ProcessingRange.FromLength(0, 3),
            ProcessingRange.FromLength(3, 3),
            ProcessingRange.FromLength(6, 3),
            ProcessingRange.FromLength(9, 5),
        };

        CollectionAssert.AreEqual(expected, slices);
    }

    [Test]
    public void FillRemainderEvenly_SlicingInArrayCenter()
    {
        var slices = ProcessingRange.FromLength(1, 13).Slice(3, SliceDistribution.FillRemainderEvenly);

        var expected = new ProcessingRange[]
        {
            ProcessingRange.FromLength(1, 5),
            ProcessingRange.FromLength(6, 4),
            ProcessingRange.FromLength(10, 4)
        };

        CollectionAssert.AreEqual(expected, slices);
    }

    [Test]
    public void FillRemainderEvenly_SlicingWithoutRemainder()
    {
        var source = Enumerable.Range(0, 15).ToArray();

        var slices = ProcessingRange.From(source).Slice(3, SliceDistribution.FillRemainderEvenly);

        var expected = new ProcessingRange[]
        {
            ProcessingRange.FromLength( 0, 5),
            ProcessingRange.FromLength( 5, 5),
            ProcessingRange.FromLength(10, 5)
        };

        CollectionAssert.AreEqual(expected, slices);
    }

    [Test]
    public void FillRemainderEvenly_Valid()
    {
        var source = Enumerable.Range(0, 14).ToArray();

        var slices = ProcessingRange.From(source).Slice(4, SliceDistribution.FillRemainderEvenly);

        var expected = new ProcessingRange[]
        {
            ProcessingRange.FromLength( 0, 4),
            ProcessingRange.FromLength( 4, 4),
            ProcessingRange.FromLength( 8, 3),
            ProcessingRange.FromLength(11, 3),
        };

        CollectionAssert.AreEqual(expected, slices);
    }
}
