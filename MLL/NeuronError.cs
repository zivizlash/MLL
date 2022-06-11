namespace MLL;

public struct NeuronError
{
    public double Error;
    public double Output;
    
    public NeuronError(double error, double output)
    {
        Error = error;
        Output = output;
    }
}
