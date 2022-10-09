namespace MLL.Common.Factory;

public interface ILayerComputerFactory
{
    bool IsCanResolve(Type type);
    FactoryResolveResult Resolve(Type type, FactoryResolveParams arg);
}
