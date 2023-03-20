using MLL.Common.Layer;
using MLL.Common.Engines;

namespace MLL.Common.Tools;

public class Throw
{
    public static void ArgumentOutOfRange(string name) => throw new ArgumentOutOfRangeException(name);
    public static void ArgumentOutOfRange(string name, string msg) => throw new ArgumentOutOfRangeException(name, msg);
    public static void Argument(string message, string paramName) => throw new ArgumentOutOfRangeException(message, paramName);
    public static void InvalidOperation(string message) => throw new InvalidOperationException(message);
    public static void ArgumentNull(string paramName) => throw new ArgumentNullException(paramName);
    public static void Disposed(string objectName) => throw new ObjectDisposedException(objectName);
}

public class Check
{
    public static void BufferFit(NetLayersBuffers buffers, LayerWeights[] weights, string paramName)
    {
        if (!buffers.IsFitWeights(weights)) Throw.Argument("Buffer has wrong size", paramName);
    }

    public static void LengthEqual(int expected, int actual, string name)
    {
        if (expected != actual)
        {
            var msg = $"Specified argument was out of the range of valid values; Expected {expected}; Actual: {actual}";
            Throw.ArgumentOutOfRange(name, msg);
        }
    }

    public static void NotNull(object? obj, string paramName)
    {
        if (obj == null) Throw.ArgumentNull(paramName);
    }
}
