namespace MLL.Statistics.Collection;

public class NeuronErrorStats
{
    public float[] Errors { get; }

    public NeuronErrorStats(float[] errors)
    {
        Errors = errors;
    }
}
