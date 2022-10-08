namespace MLL.Layer.Factories;

public interface ILayerComputerFactory
{
    bool IsCanResolve(Type type);
    FactoryResolveResult Resolve(Type type, FactoryResolveParams arg);
}
