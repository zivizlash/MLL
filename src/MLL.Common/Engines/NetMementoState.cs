using MLL.Common.Engines;
using MLL.Common.Pooling;
using MLL.Common.Tools;

namespace MLL.Common.Engines;

public readonly struct NetMementoState
{
    private readonly Pooled<NetWeights> _weights;
    private readonly ClassificationEngine _net;

    public NetMementoState(Pooled<NetWeights> weights, ClassificationEngine net)
    {
        _weights = weights;
        _net = net;
    }

    public void Apply()
    {
        if (_weights.IsReturned)
        {
            Throw.InvalidOperation("State disposed");
        }

        var temp = _net.Weights;
        _net.Weights = _weights.Value;
        _weights.ReplaceReturn(temp);
    }

    public void Forget()
    {
        _weights.Return();
    }
}
