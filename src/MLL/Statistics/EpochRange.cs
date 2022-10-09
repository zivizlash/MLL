namespace MLL.Statistics;

public struct EpochRange
{
    public int Start { get; set; }
    public int End { get; set; }

    public EpochRange(int start, int end)
    {
        Start = start;
        End = end;
    }
}
