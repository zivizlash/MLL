namespace MLL.Layer.Computers;

public class LayerErrorCalcWorkItem : IHasExecuteDelegate
{
    public float[] Outputs;
    public float[] Expected;
    public float[] Errors;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

    public LayerErrorCalcWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        for (int neuronIndex = start; neuronIndex < end; neuronIndex++)
        {
            Errors[neuronIndex] = Expected[neuronIndex] - Outputs[neuronIndex];
        }

        Countdown.Signal();
    }
}
