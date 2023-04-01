using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Factory;
using MLL.Common.Optimization;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
using MLL.ThreadingOptimization;

public class RaceImageRecognitionNetBuilder : NetFactory
{
    public override LayerComputerBuilderResult GetComputers(bool isForTrain)
    {
        var settings = new ThreadingOptimizatorFactorySettings(10000, 0.2f, 12);
        var optimizatorFactory = new ThreadingOptimizatorFactory(settings);
        var computerFactory = new BasicLayerComputerFactory(optimizatorFactory);

        return new LayerComputerBuilder(computerFactory)
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build();
    }

    public override LayerWeightsDefinition[] GetDefinitions() =>
        LayerWeightsDefinition.Builder
            .WithInputLayer(24 * 32, 240 * 320 + 1)
            .WithLayer(100)
            .WithLayer(100)
            .WithLayer(2)
            .Build();
}

public static class Program
{
    private static void Main()
    {
    }
}
