namespace MLL.Neurons;

public struct NeuronError
{
    public float Error;
    public float Output;
    
    public NeuronError(float error, float output)
    {
        Error = error;
        Output = output;
    }
}
