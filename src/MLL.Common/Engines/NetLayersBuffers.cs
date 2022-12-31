using MLL.Common.Layer;

namespace MLL.Common.Engines;

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
            int size = weightsCounts[i];
            Outputs[i] = new float[size];
            Errors[i] = new float[size];
        }
    }

    public static NetLayersBuffers CreateByWeights(IEnumerable<LayerWeights> weights)
    {
        return new(weights.Select(w => w.Weights.Length).ToArray());
    }

    public bool IsFitWeights(LayerWeights[] weights)
    {
        if (Outputs.Length != weights.Length || Errors.Length != weights.Length)
        {
            return false;
        }

        for (int i = 0; i < weights.Length; i++)
        {
            var output = Outputs[i];
            var error = Errors[i];

            var length = weights[i].Weights.Length;

            if (output.Length != length || error.Length != length)
            {
                return false;
            }
        }

        return true;
    }
}
