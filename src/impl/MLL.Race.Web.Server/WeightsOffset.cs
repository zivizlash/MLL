using MLL.Common.Engines;

namespace MLL.Race.Web.Server;

public class WeightsOffsetStats
{
    private readonly Dictionary<int, float> _stepsToScore;
    private readonly float _step;
    private readonly Stack<int> _variants;

    public bool HasOffsetsVariants => _variants.Count > 0;

    public WeightsOffsetStats(int resolution, float sourceScore, float changedScore)
    {
        _step = 1.0f / resolution;

        _stepsToScore = new(2)
        {
            [0] = sourceScore,
            [resolution] = changedScore
        };

        _variants = new(GenerateOffsets(resolution, resolution * 2));
    }

    private static IEnumerable<int> GenerateOffsets(int resolution, int count)
    {
        for (int i = 1; i < count; i++)
        {
            if (i != resolution) yield return i;
        }
    }

    public (float, float) GetBest()
    {
        var item = _stepsToScore.MaxBy(s => s.Value);

        return (item.Key * _step, item.Value);
    }

    public void Add(int variant, float score)
    {
        _stepsToScore.Add(variant, score);
    }

    public (int, float) GetNextOffset()
    {
        return ToVariant(_variants.Pop());
    }

    private (int, float) ToVariant(int value) => (value, value * _step);
}

public struct WeightsOffsetContext
{
    public float TimesApplied { get; set; }
    public NetWeights Weights { get; set; }
    public NetWeights Offsets { get; set; }

    public WeightsOffsetContext(NetWeights weights, NetWeights offsets, float timesApplied = 1)
    {
        Weights = weights;
        Offsets = offsets;
        TimesApplied = timesApplied;
    }

    public void Apply(float times)
    {
        var multiplier = times - TimesApplied;

        var weightsLayers = Weights.Layers;
        var offsetLayers = Offsets.Layers;

        for (int layerIndex = 0; layerIndex < weightsLayers.Length; layerIndex++)
        {
            var weightsLayer = weightsLayers[layerIndex].Weights;
            var offsetLayer = offsetLayers[layerIndex].Weights;

            for (int neuronIndex = 0; neuronIndex < weightsLayer.Length; neuronIndex++)
            {
                var weights = weightsLayer[neuronIndex];
                var offset = offsetLayer[neuronIndex];

                for (int weightIndex = 0; weightIndex < weights.Length; weightIndex++)
                {
                    weights[weightIndex] += offset[weightIndex] * multiplier;
                }
            }
        }

        TimesApplied = times;
    }
}

public readonly struct WeightsOffset
{
    public NetWeights Offsets { get; }

    public WeightsOffset(NetWeights offsets)
    {
        Offsets = offsets;
    }

    public WeightsOffsetContext CreateContext(NetWeights weights, float timesApplied = 1)
    {
        return new WeightsOffsetContext(weights, Offsets, timesApplied);
    }
}
