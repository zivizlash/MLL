using ML.Race.Web.Server.Handler;
using MLL.Common.Branching;
using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Factory;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
using MLL.Network.Builders;
using MLL.Network.Factories;
using MLL.Race.Web.Common.Messages;
using MLL.ThreadingOptimization;
using System.Net;

public class TestNetFactory : NetFactory
{
    public override LayerComputerBuilderResult GetComputers(bool isForTrain) =>
        new LayerComputerBuilder(GetComputerFactory())
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build(isForTrain);

    public override LayerWeightsDefinition[] GetDefinitions() =>
        LayerWeightsDefinition.Builder
            .WithInputLayer(10, 1024)
            .WithLayer(10)
            .WithLayer(10)
            .Build();

    private static ILayerComputerFactory GetComputerFactory() =>
        new BasicLayerComputerFactory(GetThreadingOptimizator());

    private static ThreadingOptimizatorFactory GetThreadingOptimizator() =>
        new ThreadingOptimizatorFactory(new(10000, 0.05f, Environment.ProcessorCount));
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var offset = new NetBranchOffsetValue(0.1f);
        var branches = new NetBranches(5, true, offset, new TestNetFactory());

    }

    private static async Task ServeAsync()
    {
        var typesProvider = new MessageTypesProvider();

        foreach (var type in typesProvider.GetTypes())
        {
            Console.WriteLine(type.FullName);
        }

        using var server = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(new ReflectionHandlerFactory<ServerMessageHandler>())
            .WithUsedTypes(typesProvider)
            .BuildServer();

        await server.WorkingTask;
    }
}
