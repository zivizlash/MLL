namespace MLL.Common.Tools;

public class ThreadedRandom
{
    private readonly List<Random> _list;
    private readonly int _seed;

    public ThreadedRandom(int seed)
    {
        _list = new();
        _seed = seed;
    }

    public void Advance(int index)
    {
        if (index > Environment.ProcessorCount * 2)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        while (_list.Count <= index)
        {
            _list.Add(new Random(_seed + index));
        }
    }

    public Random Get(int index)
    {
        if (index >= _list.Count) Advance(index);

        return _list[index];
    }
}
