using System;

namespace MLL.Network.Tools;

internal static class Throw
{
    public static void Disposed(string objectName)
    {
        throw new ObjectDisposedException(objectName);
    }
}
