using MLL.Common.Engines;
using MLL.Common.Layer;

namespace MLL.Race.Web.Server;

public enum NetLearningState
{
    Searching = 1,
    Rasterizing = 2
}

public class NetLearningContext
{
    public NetLearningState State { get; private set; }

    private readonly RaceNet _net;
    private readonly RaceNet _referenceNet;

    private readonly AdaptiveLearningRate _learningRateContext;
    private readonly WeightsRasterizer _rasterizer;

    private WeightsOffsetContext _offsetsContext;
    private WeightsOffsetStats? _offsetStats;
    private int _currentVariant;

    private int _updatingLayer;

    public NetLearningContext(RaceNetFactory factory)
    {
        _updatingLayer = 0;
        _learningRateContext = new(0.25f, 5, 25, 0.1f);
        _rasterizer = new();
        _net = new(factory, new Random());
        _referenceNet = new(factory, new Random());

        State = NetLearningState.Searching;

        NetReplicator.CopyWeights(_referenceNet.LayerWeights, _net.LayerWeights);
    }

    public (float forward, float left) Recognize(float[] image)
    {
        //Console.WriteLine("Starting recognizing");
        var input = PredictionCalculator.Predict(_net.PredictContext, image);
        //Console.WriteLine("Stopped recognizing");
        return (input[0], input[1]);
    }

    public void UpdateScore(float score)
    {
        //Console.WriteLine("Starting score updating");
        _net.Score = score;

        switch (State)
        {
            case NetLearningState.Searching:
                RaceNet src, dst;

                float learningRate;

                if (_net.Score >= _referenceNet.Score)
                {
                    (src, dst) = (_net, _referenceNet);
                    _learningRateContext.SelectNewAndGet();
                    Console.WriteLine($"Updated net was selected; LearningRate: {_learningRateContext.LearningRate}");

                    LayerWeights[]? layerWeights = default;

                    var offsets = _rasterizer.FindOffset(new(src.LayerWeights), new(dst.LayerWeights), ref layerWeights);
                    _offsetsContext = offsets.CreateContext(new(src.LayerWeights));

                    _offsetStats = new(4, _referenceNet.Score, _net.Score);
                    var (variant, offsetTimes) = _offsetStats.GetNextOffset();

                    _currentVariant = variant;
                    _offsetsContext.Apply(offsetTimes);
                    State = NetLearningState.Rasterizing;
                }
                else
                {
                    (src, dst) = (_referenceNet, _net);
                    Console.WriteLine($"Old net was selected; LearningRate: {_learningRateContext.LearningRate}");
                    learningRate = _learningRateContext.SelectOldAndGet();

                    dst.Score = src.Score;
                    NetReplicator.CopyLayer(new(src.LayerWeights), new(dst.LayerWeights), _updatingLayer);
                    IncrementUpdatingLayer();

                    _net.UpdateLearningRate(learningRate);
                    ReinforcementTrainer.RandomizeWeights(_net.ReinforcementTrainContext, _updatingLayer);
                }
                break;

            case NetLearningState.Rasterizing:
                _offsetStats!.Add(_currentVariant, score);

                if (_offsetStats.HasOffsetsVariants)
                {
                    var (variant, offsetTimes) = _offsetStats.GetNextOffset();

                    _currentVariant = variant;
                    Console.WriteLine($"Testing with offset times: {offsetTimes}; Score: {score}");
                    _offsetsContext.Apply(offsetTimes);
                }
                else
                {
                    var (offsetTimes, bestScore) = _offsetStats.GetBest();
                    Console.WriteLine($"Applied with offset times: {offsetTimes}; Score: {bestScore}");
                    _offsetsContext.Apply(offsetTimes);
                    NetReplicator.CopyLayer(new(_net.LayerWeights), new(_referenceNet.LayerWeights), _updatingLayer);
                    IncrementUpdatingLayer();

                    _net.Score = bestScore;
                    _referenceNet.Score = bestScore;

                    learningRate = _learningRateContext.SelectNewAndGet();
                    _net.UpdateLearningRate(learningRate);
                    ReinforcementTrainer.RandomizeWeights(_net.ReinforcementTrainContext, _updatingLayer);
                    State = NetLearningState.Searching;
                }
                break;

            default:
                throw new InvalidOperationException("Invalid NetLearningState");
        };

        //Console.WriteLine("Stopped score updating");
    }

    private void IncrementUpdatingLayer()
    {
        _updatingLayer = (_updatingLayer + 1) % _referenceNet.LayerComputers.Length;
    }
}
