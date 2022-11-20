using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Listening;

public class ListenerMessageHandlerPipe
{
    private readonly CancellationTokenSource _tokenSource;
    private readonly IMessageHandler _messageHandler;
    private readonly MessageConverter _messageConverter;
    private readonly MessageTcpProtocol _protocol;

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

        while (!token.IsCancellationRequested)
        {
            var raw = await _protocol.ReadAsync(token);
            var message = _messageConverter.Deserialize(raw.Data, raw.MessageType);
            await _messageHandler.HandleAsync(message);
        }
    }

    public void Stop()
    {
        _tokenSource.Cancel();
    }
}
