using MLL.Builders;
using MLL.Tools;

namespace MLL.Neurons;

// ReSharper disable SuggestBaseTypeForParameter

public class Net
{
    public NeuronLayer[] Layers { get; set; }
    public NetMemoryBuffers Buffers { get; }

    // Use benchmark?
    private const int ThreadingThreshold = 500;
    
    public Net()
    {
        Layers = Array.Empty<NeuronLayer>();
        Buffers = new NetMemoryBuffers();
    }

    public LayerDefinition[] GetDefinition()
    {
        var definition = new LayerDefinition[Layers.Length];

        for (int i = 0; i < Layers.Length; i++)
        {
            var layer = Layers[i];
            var neuron = layer.Neurons[0];

            definition[i] = new LayerDefinition(1, layer.Count, 
                neuron.Weights.Length, neuron.UseActivationFunc);
        }

        return definition;
    }
    
    public Net(float learningRate, params LayerDefinition[] definitions) : this()
    {
        Layers = new NeuronLayer[definitions.Sum(d => d.Layers)];

        int index = 0;

        foreach (var definition in definitions)
        {
            for (int layer = 0; layer < definition.Layers; layer++)
            {
                Layers[index++] = new NeuronLayer(definition.NeuronsCount,
                    definition.WeightsCount, learningRate, definition.UseActivationFunc);
            }
        }
    }

    public Net UpdateLearningRate(float learningRate)
    {
        foreach (var layer in Layers)
            foreach (var neuron in layer.Neurons)
                neuron.LearningRate = learningRate;

        return this;
    }

    public float[] Predict(float[] input)
    {
        var output = input;

        foreach (var neuronLayer in Layers)
            output = neuronLayer.Predict(output);

        return output;
    }

    private float[][] PredictWithOutputs(float[] input)
    {
        var outputs = Buffers.GetOutputsBuffer(Layers.Length);
        var currentOutput = input;

        for (var i = 0; i < Layers.Length; i++)
        {
            var neuronLayer = Layers[i];
            currentOutput = neuronLayer.Predict(currentOutput);
            outputs[i] = currentOutput;
        }

        return outputs;
    }

    public float[] Train(float[] input, float[] expected)
    {
        Buffers.EnsureNeuronErrorsCount(Layers.Length - 1);

        var outputsWith = PredictWithOutputs(input);
        var intermediate = CreateIntermediateValuesModel(outputsWith, input);

        var (errors, outputError) = CalculateGeneralErrorsAndCompensate(
            Layers.Length - 2, Layers[^1], intermediate[^1], expected);
        
        for (int layerIndex = Layers.Length - 2; layerIndex >= 0; layerIndex--)
        {
            float[] localInput = intermediate[layerIndex];
            float[] localOutput = intermediate[layerIndex + 1];
            
            bool isLastLayer = layerIndex == 0;

            errors = CompensateAndReorganizeErrors(
                layerIndex, Layers[layerIndex], errors, localOutput, localInput, isLastLayer)!;
        }
        
        Buffers.ClearNeuronErrorsBuffers();
        return outputError;
    }
    
    private float[]? CompensateAndReorganizeErrors(int layerIndex, NeuronLayer layer, 
        float[] previousErrors, float[] previousOutput, float[] input, bool isLastLayer)
    {
        int weightsCount = layer.Neurons[0].Weights.Length;

        var nextLayerErrors = isLastLayer 
            ? null 
            : Buffers.GetErrorBuffer(layerIndex - 1, weightsCount);

        if (false) // weightsCount * layer.Count > ThreadingThreshold)
        {
            Parallel.For(0, layer.Neurons.Length, i =>
            {
                var neuron = layer.Neurons[i];
                var error = new NeuronError(previousErrors[i], previousOutput[i]);

                if (nextLayerErrors != null)
                    UpdateLayerErrorsAtomic(neuron, error.Error, nextLayerErrors);

                neuron.CompensateError(input, error);
            });
        }
        else
        {
            for (int i = 0; i < layer.Neurons.Length; i++)
            {
                var neuron = layer.Neurons[i];
                var error = new NeuronError(previousErrors[i], previousOutput[i]);

                if (nextLayerErrors != null)
                    UpdateLayerErrors(neuron, error.Error, nextLayerErrors);

                neuron.CompensateError(input, error);
            }
        }
        
        return nextLayerErrors;
    }

    private (float[] backpropError, float[] outputError) CalculateGeneralErrorsAndCompensate(
        int layerIndex, NeuronLayer layer, float[] previousInput, float[] expected)
    {
        var neurons = layer.Neurons;
        var weightsCount = neurons[0].Weights.Length;

        var outputErrorBuffer = Buffers.GetLastLayerBuffer(layer.Count);
        var nextErrors = Buffers.GetErrorBuffer(layerIndex, weightsCount);

        for (int neuronIndex = 0; neuronIndex < neurons.Length; neuronIndex++)
        {
            var neuron = neurons[neuronIndex];
            var error = neuron.CalculateError(previousInput, expected[neuronIndex]);
            outputErrorBuffer[neuronIndex] = error.Error;

            UpdateLayerErrors(neuron, error.Error, nextErrors);
            neuron.CompensateError(previousInput, error);
        }
        
        return (nextErrors, outputErrorBuffer);
    }

    private static void UpdateLayerErrorsAtomic(SigmoidNeuron neuron,
        float currentLayerError, float[] nextLayerErrors)
    {
        var weightsSum = neuron.CalculateWeightsSum();

        for (int weightIndex = 0; weightIndex < neuron.Weights.Length; weightIndex++)
        {
            var weightValue = neuron.Weights[weightIndex];
            var errorPart = Math.Abs(weightValue) / weightsSum * currentLayerError;
            // There is no Interlocked.Add float overload
            NumberTools.AtomicAdd(ref nextLayerErrors[weightIndex], errorPart);
        }
    }

    // Optimize nextLayerErrors buffer zeroing?
    private static void UpdateLayerErrors(SigmoidNeuron neuron, 
        float currentLayerError, float[] nextLayerErrors)
    {
        var weightsSum = neuron.CalculateWeightsSum();

        for (int weightIndex = 0; weightIndex < neuron.Weights.Length; weightIndex++)
        {
            var weightValue = neuron.Weights[weightIndex];
            var errorPart = Math.Abs(weightValue) / weightsSum * currentLayerError;
            nextLayerErrors[weightIndex] += errorPart;
        }
    }

    private float[][] CreateIntermediateValuesModel(float[][] layersOutputs, float[] firstLayerInput)
    {
        var outputs = Buffers.GetIntermediateValuesBuffer(layersOutputs.Length);
        outputs[0] = firstLayerInput;

        for (int i = 0; i < layersOutputs.Length - 1; i++)
            outputs[i + 1] = layersOutputs[i];

        return outputs;
    }

    public Net FillRandomValues(Random random, double range = 0.5)
    {
        foreach (var layer in Layers)
        {
            foreach (var neuron in layer.Neurons)
                neuron.FillRandomValues(random, range);
        }

        return this;
    }
}
