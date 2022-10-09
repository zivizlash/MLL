namespace MLL.Common.Factory;

public struct FactoryResolveParams
{
    public int MaxThreads;
    public float OutlinersThreshold;
    public int RequiredSamples;
    public bool IsRequiredErrorBackpropagation;
    public bool IsRequiredErrorCalculation;
    public bool IsRequiredCompensate;
}
