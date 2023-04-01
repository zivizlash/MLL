namespace MLL.Statistics.Collection;

public class NeuronErrorStats
{
    public float[] Errors { get; }

    [Obsolete]
    public NeuronErrorStats()
    {
        Errors = Array.Empty<float>();
    }

    public NeuronErrorStats(float[] errors)
    {
        Errors = errors;
    }
}
