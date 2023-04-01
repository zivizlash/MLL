namespace MLL.Repository.DataModels;

public class NetInfoLastUpdateData
{
    public DateTime LastUpdate { get; set; }

    public NetInfoLastUpdateData()
    {
        LastUpdate = DateTime.Now;
    }
}
