namespace MLL.Neurons;

public class NetMemoryBuffers
{
    private float[]? _lastLayerOutputBuffer;
    private float[][]? _outputsBuffer;
    private float[][]? _intermediateValuesBuffer;
    private float[][]? _neuronErrorsBuffers;

    public float[] GetLastLayerBuffer(int length)
    {
        if (_lastLayerOutputBuffer?.Length != length)
            _lastLayerOutputBuffer = new float[length];
        
        return _lastLayerOutputBuffer;
    }

    public float[] GetLastLayerBufferRaw()
    {
        return _lastLayerOutputBuffer;
    }

    public float[][] GetOutputsBuffer(int length)
    {
        if (_outputsBuffer?.Length != length)
            _outputsBuffer = new float[length][];

        return _outputsBuffer;
    }

    public float[][] GetIntermediateValuesBuffer(int length)
    {
        if (_intermediateValuesBuffer?.Length != length)
            _intermediateValuesBuffer = new float[length][];

        return _intermediateValuesBuffer;
    }

    public void EnsureNeuronErrorsCount(int count)
    {
        if (_neuronErrorsBuffers?.Length != count)
            _neuronErrorsBuffers = new float[count][];
    }
    
    public float[] GetErrorBuffer(int layer, int count)
    {
        var buffer = _neuronErrorsBuffers[layer];

        if (buffer?.Length != count)
            buffer = _neuronErrorsBuffers[layer] = new float[count];

        return buffer;
    }
    
    public void ClearNeuronErrorsBuffers()
    {
        foreach (var neuronErrorBuffer in _neuronErrorsBuffers)
            Array.Fill(neuronErrorBuffer, 0);
    }
}
