using MLL.Layer;
using MLL.Layer.Backpropagation;
using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;
using MLL.Tools;

namespace MLL.Neurons;

public class NetManager
{
    private readonly LayerComputers[] _layersComputers;
    private readonly LayerWeights[] _layersWeights;
    private readonly OptimizationManager _optimizationManager;
    private readonly NetLayersBuffers _buffers;

    public ReadOnlySpan<LayerWeights> Weights => _layersWeights;
    public ReadOnlySpan<LayerComputers> Computers => _layersComputers;
    public OptimizationManager OptimizationManager => _optimizationManager;

    public NetManager(LayerComputers[] layersComputers, LayerWeights[] layersWeights, 
        OptimizationManager optimizationManager)
    {
        Check.LengthEqual(layersComputers.Length, layersWeights.Length, nameof(layersWeights));

        var weightsCount = layersWeights.Select(lw => lw.Neurons.Length).ToArray();
        _buffers = new NetLayersBuffers(weightsCount);

        _layersComputers = layersComputers;
        _layersWeights = layersWeights;
        _optimizationManager = optimizationManager;
    }

    public ReadOnlySpan<float> Train(float[] input, float[] expected, float learningRate)
    {
        Check.LengthEqual(_layersWeights[0].Neurons[0].Length, input.Length, nameof(input));
        Check.LengthEqual(expected.Length, _layersWeights[^1].Neurons.Length, nameof(expected));

        float[] output = PredictInternal(input);
        float[] outputErrors = CalculateAndCompensateOutputLayerError(input, output, expected, learningRate);
        
        for (int bi = _buffers.Outputs.Length - 2; bi >= 0; bi--)
        {
            float[] layerInput = bi == 0 ? input : _buffers.Outputs[bi - 1];
            CompensateLayerError(layerInput, bi, learningRate);
        }

        _optimizationManager.Optimize();
        return outputErrors;
    }

    public ReadOnlySpan<float> Predict(float[] input)
    {
        Check.LengthEqual(_layersWeights[0].Neurons[0].Length, input.Length, nameof(input));
        var prediction = PredictInternal(input);

        _optimizationManager.Optimize();
        return prediction;
    }
    
    private void CompensateLayerError(float[] input, int layerIndex, float lr)
    {
        var layer = _layersWeights[layerIndex];
        var previousLayer = _layersWeights[layerIndex + 1];
        var output = _buffers.Outputs[layerIndex];

        var previousErrors = _buffers.Errors[layerIndex + 1];
        var errors = _buffers.Errors[layerIndex];

        var compensate = _layersComputers[layerIndex].Compensate;
        var errorBackprop = _layersComputers[layerIndex + 1].ErrorBackpropagation;

        errorBackprop.ReorganizeErrors(new(previousLayer.Neurons, previousErrors), errors);
        compensate.Compensate(layer, input, lr, errors, output);
    }

    private float[] CalculateAndCompensateOutputLayerError(float[] baseInput, float[] output, float[] expected, float lr)
    {
        float[] errorBuffer = _buffers.Errors[^1];
        float[] previousOutput = _buffers.Outputs.Length < 2 
            ? baseInput 
            : _buffers.Outputs[^2];

        var computer = _layersComputers[^1];

        computer.Calculate.CalculateErrors(output, expected, errorBuffer);
        computer.Compensate.Compensate(_layersWeights[^1], previousOutput, lr, errorBuffer, output);

        return errorBuffer;
    }

    private float[] PredictInternal(float[] input)
    {
        float[] layerInput = input;
        float[][] buffers = _buffers.Outputs;

        for (int i = 0; i < _layersComputers.Length; i++)
        {
            var layer = _layersComputers[i];
            var weights = _layersWeights[i];
            float[] buffer = buffers[i];

            layer.Predict.Predict(weights, layerInput, buffer);
            layerInput = buffer;
        }

        float[] lastLayerOutput = layerInput;
        return lastLayerOutput;
    }
}
