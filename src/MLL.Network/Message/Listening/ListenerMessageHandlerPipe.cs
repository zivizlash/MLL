using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Listening;

public class ListenerMessageHandlerPipe
{
    private readonly IMessageHandler _messageHandler;
    private readonly MessageConverter _messageConverter;
    private readonly IMessageProtocol _protocol;
    private readonly RemoteConnectionInfo _connectionInfo;
    private readonly CancellationTokenSource _tokenSource;

    public ListenerMessageHandlerPipe(IMessageHandler messageHandler, MessageConverter messageConverter, 
        IMessageProtocol protocol, RemoteConnectionInfo connectionInfo)
    {
        _messageHandler = messageHandler;
        _messageConverter = messageConverter;
        _protocol = protocol;
        _connectionInfo = connectionInfo;
        _tokenSource = new();
        Task.Run(Listen);
    }

    private async Task Listen()
    {
        var token = _tokenSource.Token;

        try
        {
            for (; ;)
            {
                token.ThrowIfCancellationRequested();

                RawMessage? raw = default;
                object? message = default;

                try
                {
                    raw = await _protocol.ReadAsync(token);
                    var msg = raw.Value;
                    message = _messageConverter.Deserialize(msg.Data.Value, 0, msg.Length, msg.MessageType);
                }
                finally
                {
                    raw?.Data.Dispose();
                }

                await _messageHandler.HandleAsync(message).ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException ex)
        {
            await _connectionInfo.DisconnectWithErrorAsync(ex);
        }
        catch (OperationCanceledException)
        {
            await _connectionInfo.DisconnectAsync();
        }
        catch (Exception ex)
        {
            await _connectionInfo.DisconnectWithErrorAsync(ex);
        }
    }

    public ValueTask StopAsync()
    {
        _tokenSource.Cancel();
        return _connectionInfo.DisconnectAsync();
    }
}
