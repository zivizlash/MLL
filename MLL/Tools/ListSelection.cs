namespace MLL.Tools;

public static class ListSelection
{
    public static ListSelection<int> Range(int count)
    {
        return Range(1, count);
    }

    public static ListSelection<int> Range(int start, int count)
    {
        return new ListSelection<int>(Enumerable.Range(start, count));
    }
}

public struct ListSelection<T>
{
    private int _currentIndex;

    public List<T> Source { get; }
    public T Current => Source[CurrentIndex];

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (value < 0 || value >= Source.Count) throw new ArgumentOutOfRangeException(nameof(value));
            _currentIndex = value;
        }
    }

    public ListSelection(List<T> source)
    {
        if (source.Count == 0) throw new ArgumentOutOfRangeException(nameof(source));

        Source = source;
        _currentIndex = 0;
    }

    public ListSelection(IEnumerable<T> source) : this(source.ToList())
    {
    }

    public T GetNext()
    {
        if (!TryGetNext(out var next))
        {
            throw new InvalidOperationException();
        }

        return next;
    }

    public bool TryGetNext(out T? value)
    {
        if (_currentIndex + 1 >= Source.Count)
        {
            value = default;
            return false;
        }

        value = Source[_currentIndex++];
        return true;
    }
}
