using MLL.Common.Engines;
using MLL.Common.Factory;
using MLL.Common.Layer;
using Newtonsoft.Json;

namespace MLL.Race.Web.Server;

public enum NetLearningState
{
    Searching = 1,
    Rasterizing = 2
}

public class NetSaver
{
    private readonly string _folder;

    public int LastLoadedGen { get; private set; }
    public float LastLoadedScore { get; private set; }

    public NetSaver(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);
    }

    public void Save(NetInstance instance)
    {
        var filePath = Path.Combine(_folder, $"net_save_{instance.Gen}.json");

        var json = JsonConvert.SerializeObject(instance);
        File.WriteAllText(filePath, json);
    }

    public NetInstance? Load()
    {
        var file = Directory.EnumerateFiles(_folder)
            .Where(filePath => Path.GetFileName(filePath).StartsWith("net_save_"))
            .Select(filePath => new 
            { 
                filePath, 
                gen = int.Parse(Path.GetFileNameWithoutExtension(filePath).Split("_")[2]) 
            })
            .OrderByDescending(x => x.gen)
            .FirstOrDefault();

        if (file == null)
        {
            return null;
        }

        var json = File.ReadAllText(file.filePath);

        var writableInstance = JsonConvert.DeserializeObject<WritableNetInstance>(json)
            ?? throw new InvalidOperationException();

        LastLoadedGen = writableInstance.Gen;
        LastLoadedScore = writableInstance.Score;

        return new NetInstance
        {
            Gen = writableInstance.Gen,
            Score = writableInstance.Score,
            Weights = writableInstance.Weights.Select(w => new LayerWeights(w.Weights)).ToArray()
        };
    }

    public class NetInstance
    {
        public float Score { get; set; }
        public int Gen { get; set; }
        public LayerWeights[] Weights { get; set; }

        public NetInstance()
        {
            Weights = Array.Empty<LayerWeights>();
        }
    }

    public class WritableLayerWeights
    {
        public float[][] Weights;

        public WritableLayerWeights()
        {
            Weights = Array.Empty<float[]>();
        }
    }

    public class WritableNetInstance
    {
        public float Score { get; set; }
        public int Gen { get; set; }
        public WritableLayerWeights[] Weights { get; set; }

        public WritableNetInstance()
        {
            Weights = Array.Empty<WritableLayerWeights>();
        }
    }
}

public class NetLearningContext
{
    public NetLearningState State { get; private set; }

    private readonly RaceNet _net;
    private readonly RaceNet _referenceNet;

    private readonly AdaptiveLearningRate _learningRateContext;
    private readonly WeightsRasterizer _rasterizer;
    private readonly NetSaver _netSaver;

    private WeightsOffsetContext _offsetsContext;
    private WeightsOffsetStats? _offsetStats;

    private int _currentVariant;
    private int _updatingLayer;
    private int _generation;

    public NetLearningContext(NetFactory factory, NetSaver netSaver)
    {
        _updatingLayer = 0;
        _learningRateContext = new(10f, 1, 100, 0.001f);
        _rasterizer = new();
        _net = new(factory, new Random());
        _referenceNet = new(factory, new Random());
        _netSaver = netSaver;

        _generation = netSaver.LastLoadedGen;
        _net.Score = netSaver.LastLoadedScore;
        _referenceNet.Score = netSaver.LastLoadedScore;

        State = NetLearningState.Searching;
        NetReplicator.CopyWeights(_referenceNet.LayerWeights, _net.LayerWeights);
    }

    public (float forward, float left) Recognize(float[] image)
    {
        var input = PredictionCalculator.Predict(_net.PredictContext, image);
        return (input[0], input[1]);
    }

    public void UpdateScore(float score)
    {
        _net.Score = score;

        switch (State)
        {
            case NetLearningState.Searching:
                RaceNet src, dst;

                float learningRate;

                if (_net.Score >= _referenceNet.Score)
                {
                    (src, dst) = (_net, _referenceNet);
                    _learningRateContext.ChooseNew();

                    Console.WriteLine($"New selected; LR: {_learningRateContext.LearningRate}; Score: {_net.Score}");
                    Console.WriteLine($"Gen: {++_generation}");

                    LayerWeights[]? offsetWeights = default;

                    var offsets = _rasterizer.FindOffset(new(src.LayerWeights), new(dst.LayerWeights), ref offsetWeights);
                    _offsetsContext = offsets.CreateContext(new(src.LayerWeights));

                    _offsetStats = new(4, _referenceNet.Score, _net.Score);
                    var (variant, offsetTimes) = _offsetStats.GetNextOffset();

                    _currentVariant = variant;
                    _offsetsContext.Apply(offsetTimes);

                    _netSaver.Save(new NetSaver.NetInstance
                    {
                        Gen = _generation,
                        Weights = src.LayerWeights,
                        Score = src.Score
                    });

                    State = NetLearningState.Rasterizing;
                }
                else
                {
                    (src, dst) = (_referenceNet, _net);
                    Console.WriteLine($"Old selected; LR: {_learningRateContext.LearningRate}; Score: {_net.Score}");
                    Console.WriteLine($"Gen: {_generation}");

                    learningRate = _learningRateContext.ChooseOld();

                    dst.Score = src.Score;
                    NetReplicator.CopyLayer(new(src.LayerWeights), new(dst.LayerWeights), _updatingLayer);
                    IncrementUpdatingLayer();

                    _net.UpdateLearningRate(learningRate);
                    ReinforcementTrainer.Randomize(_net.ReinforcementTrainContext, _updatingLayer);
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

                    learningRate = _learningRateContext.ChooseNew();
                    _net.UpdateLearningRate(learningRate);
                    ReinforcementTrainer.Randomize(_net.ReinforcementTrainContext, _updatingLayer);
                    State = NetLearningState.Searching;
                }
                break;

            default:
                throw new InvalidOperationException("Invalid NetLearningState");
        };
    }

    private void IncrementUpdatingLayer()
    {
        _updatingLayer = (_updatingLayer + 1) % _referenceNet.LayerComputers.Length;
    }
}
