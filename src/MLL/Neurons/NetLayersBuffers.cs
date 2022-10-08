namespace MLL.Neurons;

public class NetLayersBuffers
{
    public float[][] Outputs;
    public float[][] Errors;

    public NetLayersBuffers(int[] weightsCounts)
    {
        Outputs = new float[weightsCounts.Length][];
        Errors = new float[weightsCounts.Length][];

        for (int i = 0; i < weightsCounts.Length; i++)
        {
            int weightsCount = weightsCounts[i];
            Outputs[i] = new float[weightsCount];
            Errors[i] = new float[weightsCount];
        }
    }
}
