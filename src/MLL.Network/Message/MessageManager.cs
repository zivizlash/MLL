using MLL.Network.Message.Handlers;
using MLL.Network.Message.Listening;
using System;

namespace MLL.Network.Message;

public class MessageManager : IDisposable
{
    private bool _disposed;
    
    private readonly MessageHandlerBusBase _messageHandler;
    private readonly RawMessageListeningPipe _listeningPipe;

    public MessageManager(MessageHandlerBusBase messageHandler, RawMessageListeningPipe listeningPipe)
    {
        _messageHandler = messageHandler;
        _listeningPipe = listeningPipe;
    }

    public void Start()
    {
        ThrowIfDisposed();
        _listeningPipe.StartOrRestart();
    }

    public void Stop()
    {
        ThrowIfDisposed();
        _listeningPipe.Stop();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MessageManager));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
