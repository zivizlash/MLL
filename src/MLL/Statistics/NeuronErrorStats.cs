namespace MLL.Statistics;

public class NeuronErrorStats
{
    public float[] Errors { get; }

    public NeuronErrorStats(float[] errors)
    {
        Errors = errors;
    }
}
