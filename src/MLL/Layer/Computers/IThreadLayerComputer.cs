using MLL.Layer.Threading;

namespace MLL.Layer.Computers;

public interface IThreadLayerComputer
{
    LayerThreadInfo CalculateThreadInfo { get; set; }
    LayerThreadInfo PredictThreadInfo { get; set; }
    LayerThreadInfo CompensateThreadInfo { get; set; }
}
