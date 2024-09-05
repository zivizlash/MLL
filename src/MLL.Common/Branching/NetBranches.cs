using MLL.Common.Engines;
using MLL.Common.Factory;
using MLL.Common.Files;
using MLL.Common.Tools;

namespace MLL.Common.Branching;

public struct NetBranchOffsetValue
{
    public float RandomOffsetValue { get; set; }

    public NetBranchOffsetValue(float value)
    {
        RandomOffsetValue = value;
    }
}

public readonly struct BranchScoreUpdate
{
    public readonly int Id;
    public readonly float Score;

    public BranchScoreUpdate(int id, float score)
    {
        Id = id;
        Score = score;
    }
}

public class NetBranches
{
    private readonly NetBranchContext[] _branches;
    private readonly NetBranchOffsetValue _offset;
    
    private readonly Random[] _randoms;

    private int LayersCount => _branches[0].Net.Weights.Layers.Length;
    public ClassificationEngine RefNet => _branches[0].Net;

    public NetBranches(int branchesCount, NetBranchOffsetValue offset, INetFactory factory)
    {
        _branches = new NetBranchContext[branchesCount];
        _randoms = new Random[branchesCount];
        _offset = offset;

        for (int i = 0; i < branchesCount; i++)
        {
            var net = factory.Create(forTrain: false);
            _branches[i] = new NetBranchContext(i, net);
            _randoms[i] = new Random(447 + i);
        }
    }

    private void CalculateNetScore(IDataSet[] dataSets, float[] results, int layerIndex, NetBranchContext branch)
    {
        throw new NotImplementedException();

        foreach (var dataSet in dataSets)
        {
            var number = dataSet.Value;

            //for (int imageIndex = 0; imageIndex < dataSet.Count; imageIndex++)
            //{
            //    var image = dataSet[imageIndex];
            //    var result = branch.Net.Predict(image.Data);

            //    for (int resultIndex = 0; resultIndex < result.Length; resultIndex++)
            //    {
            //        var resultValue = result[resultIndex];
            //        var expected = resultIndex == number ? 1.0f : -1.0f;

            //        var score = MathTools.GetCloseness(resultValue, expected, 2);
            //        results[branch.Id] += score;
            //    }
            //}
        }
    }

    public void Optimize()
    {
        foreach (var branch in _branches)
        {
            branch.Net.OptimizationManager.Optimize();
        }
    }

    public float[] Train(IDataSet[] dataSets)
    {
        var commonScore = dataSets.Sum(ds => ds.Count);
        var results = new float[_branches.Length];
        var commonResults = new float[_branches.Length];

        for (int layerIndex = LayersCount - 1; layerIndex >= 0; layerIndex--)
        {
            RandomUpdateByLayer(layerIndex);

            Parallel.ForEach(_branches, branch => CalculateNetScore(dataSets, results, layerIndex, branch));

            SelectBestByScore(results.Select((r, id) => new BranchScoreUpdate(id, r)).ToArray(), layerIndex);

            for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
            {
                commonResults[resultIndex] += results[resultIndex];
            }
            
            Array.Clear(results, 0, results.Length);
        }

        return commonResults.Select(r => r / commonScore).ToArray();
    }

    public void UpdateNets(NetWeights weights)
    {
        foreach (var branch in _branches)
        {
            NetReplicator.CopyWeights(weights.Layers, branch.Net.Weights.Layers);
        }
    }

    public void RandomUpdateLayers()
    {
        for (int layerIndex = LayersCount - 1; layerIndex >= 0; layerIndex--)
        {
            RandomUpdateByLayer(layerIndex);
        }
    }

    public void SelectBestByScore(ReadOnlySpan<BranchScoreUpdate> updates, int updatingLayer)
    {
        if (updates.Length == 0) throw new ArgumentOutOfRangeException(nameof(updates));

        var index = 0;
        var maxScore = float.MinValue;

        foreach (var update in updates)
        {
            if (update.Score > maxScore)
            {
                maxScore = update.Score;
                index = update.Id;
            }
        }

        var net = _branches[index].Net;

        for (int i = 0; i < _branches.Length; i++)
        {
            if (i == index) continue;
            NetReplicator.CopyLayer(net.Weights, _branches[i].Net.Weights, updatingLayer);
        }
    }

    public void RandomUpdateByLayer(int layerIndex)
    {
        for (int i = 1; i < _branches.Length; i++)
        {
            var branch = _branches[i];
            var random = _randoms[i];
            var layer = branch.Net.Weights.Layers[layerIndex];

            var val = _offset.RandomOffsetValue;

            foreach (var weights in layer.Weights)
            {
                for (int weightIndex = 0; weightIndex < weights.Length; weightIndex++)
                {
                    weights[weightIndex] *= (float)(1 + (random.NextDouble() * val * 2 - val));
                }
            }
        }
    }
}
