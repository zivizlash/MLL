using MLL.Common.Engines;
using MLL.Repository.Data;

namespace MLL.Repository;

public interface INetInfo
{
    string Name { get; set; }
    string Description { get; set; }
    DateTime Created { get; }
    DateTime Updated { get; }
    INetData Data { get; }

    bool HasSnapshots { get; }

    IEnumerable<INetSnapshot> Snapshots { get; }
    INetSnapshot Latest { get; }

    INetSnapshot AddSnapshot(NetWeights weights);
}
