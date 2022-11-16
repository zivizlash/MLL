using System;
using System.Threading;

namespace MLL.Network.Tools;

public struct CancellationTokenBound : IDisposable
{
    private CancellationTokenRegistration _tokenRegistration;
    private bool _disposed;

    public CancellationToken Token { get; }

    public CancellationTokenBound(CancellationToken token, CancellationTokenRegistration registration)
    {
        _disposed = false;
        _tokenRegistration = registration;
        Token = token;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _tokenRegistration.Dispose();
    }
}

public static class CancellationTokenTools
{
    public static CancellationTokenBound CombineWithTimeout(this CancellationToken token, TimeSpan timeout)
    {
        var tokenSource = new CancellationTokenSource(timeout);
        var registration = token.Register(tokenSource.Cancel);
        return new CancellationTokenBound(tokenSource.Token, registration);
    }
}
