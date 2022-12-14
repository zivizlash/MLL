namespace MLL.Common.Branching;
using Net;

public class NetBranchContext
{
    public int Id { get; }
    public float Score { get; set; }
    public Net Net { get; }
    
    public NetBranchContext(int id, Net net)
    {
        Id = id;
        Net = net;
    }
}
