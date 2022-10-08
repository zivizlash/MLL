namespace MLL.Statistics;

public class NeuronRecognizedStats
{
    public float[] Recognized { get; }
    public float Total { get; }
    public bool IsTestSet { get; }

    public NeuronRecognizedStats(float[] recognized, float total, bool isTestSet)
    {
        Recognized = recognized;
        Total = total;
        IsTestSet = isTestSet;
    }
}
