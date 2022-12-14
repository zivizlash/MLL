using MLL.Common.Layer;
using MLL.Common.Net;

namespace MLL.Common.Tools;

public class Throw
{
    public static void ArgumentOutOfRange(string name) => throw new ArgumentOutOfRangeException(name);
    public static void Argument(string message, string paramName) => throw new ArgumentOutOfRangeException(message, paramName);
    public static void InvalidOperation(string message) => throw new InvalidOperationException(message);
}

public class Check
{
    public static void BufferFit(NetLayersBuffers buffers, LayerWeights[] weights, string paramName)
    {
        if (!buffers.IsFitWeights(weights)) Throw.Argument("Buffer has wrong size", paramName);
    }

    public static void LengthEqual(int len1, int len2, string name)
    {
        if (len1 != len2) Throw.ArgumentOutOfRange(name);
    }
}
