using MLL.Common.Tools;

namespace MLL;

public class IndicesShuffler
{
    private readonly Random _random;

    public (int number, int index)[] Combinations { get; }

    public IndicesShuffler(IEnumerable<(int, int)> combinations)
    {
        _random = new Random();
        Combinations = combinations.ToArray();
    }

    public (int number, int index)[] ShuffleAndGet()
    {
        Combinations.ShuffleInPlace(_random);
        return Combinations;
    }
}
