namespace MLL.Statistics;

public class StatContainer<T>
{
    public int Epoch { get; }
    public T Value { get; }

    public StatContainer(int epoch, T value)
    {
        Epoch = epoch;
        Value = value;
    }
}
