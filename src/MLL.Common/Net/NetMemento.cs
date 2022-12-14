using MLL.Common.Pooling;

namespace MLL.Common.Net;

public class NetMemento
{
    private readonly Pool<NetWeights> _weightsPool;

    public NetMemento(Pool<NetWeights> weightsPool)
    {
        _weightsPool = weightsPool;
    }

    public NetMementoState Capture(Net net)
    {
        var weights = _weightsPool.Get();

        var src = net.Weights;
        var dest = weights.Value;

        NetReplicator.CopyWeights(src.Layers, dest.Layers);
        return new NetMementoState(weights, net);
    }
}
