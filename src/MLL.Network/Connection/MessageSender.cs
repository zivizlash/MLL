using Microsoft.Extensions.Logging;
using MLL.Network.Message.Converters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class MessageSender : IMessageSender
{
    private readonly MessageTcpProtocol _protocol;
    private readonly MessageConverter _converter;
    private readonly Guid _uid;
    private readonly ILogger<MessageSender> _logger;

    public MessageSender(MessageTcpProtocol protocol, MessageConverter converter, 
        Guid uid, ILogger<MessageSender> logger)
    {
        _protocol = protocol;
        _converter = converter;
        _uid = uid;
        _logger = logger;
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        var (messageBytes, messageType) = _converter.Serialize(message);

        try
        {
            await _protocol
                .WriteAsync(messageBytes, messageType, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uid: {Uid}; Error while sending {MessageTypeName}", _uid, typeof(T).Name);
        }
    }
}
