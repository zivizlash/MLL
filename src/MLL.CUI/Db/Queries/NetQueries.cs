using LiteDB;
using MLL.CUI.Models;

namespace MLL.CUI.Db.Queries;

public static class NetQueries
{
    public static List<NetInfo> LoadAll(this ILiteCollection<NetInfo> nets)
    {
        return nets
            .Query()
            .Include(x => x.Source)
            .Include(x => x.Structure)
            .Include(x => x.Updates)
            .OrderByDescending(x => x.Updated)
            .ToList();
    }
}
