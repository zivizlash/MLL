using MLL.Common.Engines;
using MLL.Common.Pooling;

namespace MLL.Common.Engines;

public class NetMemento
{
    private readonly Pool<NetWeights> _weightsPool;

    public NetMemento(Pool<NetWeights> weightsPool)
    {
        _weightsPool = weightsPool;
    }

    public NetMementoState Capture(ClassificationEngine net)
    {
        var weights = _weightsPool.Get();

        var src = net.Weights;
        var dest = weights.Value;

        NetReplicator.CopyWeights(src.Layers, dest.Layers);
        return new NetMementoState(weights, net);
    }
}
