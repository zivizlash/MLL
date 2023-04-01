using MLL.Common.Engines;
using MLL.Repository.Data;

namespace MLL.Repository;

public interface INetSnapshot
{
    INetData Data { get; }
    NetWeights Weights { get; set; }
}
