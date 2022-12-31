using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Listening;

public class ListenerMessageHandlerPipe
{
    private readonly CancellationTokenSource _tokenSource;
    private readonly IMessageHandler _messageHandler;
    private readonly MessageConverter _messageConverter;
    private readonly MessageTcpProtocol _protocol;

    private int _disposed;

    public ListenerMessageHandlerPipe(IMessageHandler messageHandler,
        MessageConverter messageConverter, MessageTcpProtocol protocol)
    {
        _messageHandler = messageHandler;
        _messageConverter = messageConverter;
        _protocol = protocol;
        _tokenSource = new();
        Task.Run(Listen);
    }

    private async Task Listen()
    {
        var token = _tokenSource.Token;

        //try
        //{
            while (!token.IsCancellationRequested)
            {
                var raw = await _protocol.ReadAsync(token).ConfigureAwait(false);
                var message = _messageConverter.Deserialize(raw.Data, raw.MessageType);
                await _messageHandler.HandleAsync(message).ConfigureAwait(false);
            }
        //catch (TimeoutException)
        //{

        //}
        //catch (IOException ex)
        //{
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine(ex);
        //}

        //try
        //{
        //    _protocol.Stop();
        //}
    }

    public void Stop()
    {
        _tokenSource.Cancel();
    }

    private bool IsCanDispose() => Interlocked.CompareExchange(ref _disposed, 0, 1) == 0;
}
