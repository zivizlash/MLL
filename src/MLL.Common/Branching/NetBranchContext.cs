using MLL.Common.Engines;

namespace MLL.Common.Branching;

public class NetBranchContext
{
    public int Id { get; }
    public float Score { get; set; }
    public ClassificationEngine Net { get; }
    
    public NetBranchContext(int id, ClassificationEngine net)
    {
        Id = id;
        Net = net;
    }
}
