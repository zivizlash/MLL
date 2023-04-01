using MLL.Common.Engines;
using MLL.Common.Factory;

namespace MLL.Repository.Factory;

public abstract class NetInfoFillerNetFactory : RandomFillerNetFactory
{
    private readonly INetInfo _netInfo;

    protected NetInfoFillerNetFactory(INetInfo netInfo, int randomSeed) : base(randomSeed)
    {
        _netInfo = netInfo;
    }

    public override void PostCreation(ClassificationEngine net)
    {
        if (_netInfo.HasSnapshots)
        {
            net.Weights = _netInfo.Latest.Weights;
            return;
        }

        base.PostCreation(net);
    }
}
