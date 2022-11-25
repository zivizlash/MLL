using MessagePack;
using MessagePack.Resolvers;
using MLL.Network.Builders;
using MLL.Network.Factories;
using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Listening;
using MLL.Network.Message.Protocol;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Tests;

public class ServerTestMessageHandler
{
    private readonly IMessageSender _messageSender;

    public PingMessage? LastPingMessage { get; private set; }

    public ServerTestMessageHandler(IMessageSender messageSender)
    {
        _messageSender = messageSender;
    }

    [MessageHandler]
    public async ValueTask PingHandler(PingMessage ping)
    {
        LastPingMessage = ping;
        if (ping.Count == 0) return;

        await _messageSender.SendAsync(new PongMessage 
        { 
            PongValue = ping.PingValue,
            PongSquareValue = ping.PingValue * ping.PingValue,
            Count = ping.Count - 1
        });
    }
}

public class ClientTestMessageHandler
{
    private readonly IMessageSender _sender;

    private volatile int _pongCallsCount;

    public int PongCallsCount 
    {
        get => _pongCallsCount; private set => _pongCallsCount = value;
    }

    public ClientTestMessageHandler(IMessageSender sender)
    {
        _sender = sender;
    }

    public async Task SendPing(int pingValue, int count)
    {
        await _sender.SendAsync(new PingMessage
        {
            PingValue = pingValue,
            Count = count
        });
    }

    [MessageHandler]
    public async Task PongHandler(PongMessage pong)
    {
        PongCallsCount++;

        await _sender.SendAsync(new PingMessage
        {
            PingValue = pong.PongValue,
            Count = pong.Count
        });
    }
}

[TestFixture]
public class IntegrationProtocolTest
{
    private readonly MessagePackSerializerOptions _options;

    public IntegrationProtocolTest()
    {
        _options = MessagePackSerializer.DefaultOptions
            .WithResolver(ContractlessStandardResolver.Instance);
    }

    [Test]
    public async Task ServerManagerTest()
    {
        var acceptableTypes = new List<Type> { typeof(PingMessage), typeof(PongMessage) }.ToArray();

        var serverFactory = new ReflectionHandlerFactory<ServerTestMessageHandler>();
        var serverSingleton = new SingletonHandlerFactory<ServerTestMessageHandler>(serverFactory);

        using var serverManager = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(serverSingleton)
            .WithUsedTypes(acceptableTypes)
            .BuildServer();

        var hashCode = new ProtocolVersionHashCode();
        var messageConverter = new MessageConverter(acceptableTypes, hashCode);

        var clientEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);

        var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = cancellationSource.Token;

        var clientFactory = new ReflectionHandlerFactory<ClientTestMessageHandler>();
        var clientSingleton = new SingletonHandlerFactory<ClientTestMessageHandler>(clientFactory);

        using var clientManager = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888))
            .WithHandlerFactory(clientSingleton)
            .WithUsedTypes(acceptableTypes)
            .BuildClient();

        const int pingValue = 100;
        const int pingRepeats = 5;

        await clientManager.ConnectAsync();
        await clientSingleton.Instance.SendPing(pingValue, pingRepeats);

        var spin = new SpinWait();

        for (;;)
        {
            if (clientSingleton.Instance.PongCallsCount == pingRepeats)
            {
                break;
            }

            token.ThrowIfCancellationRequested();
            spin.SpinOnce();
        }

        Assert.AreEqual(pingValue, serverSingleton.Instance.LastPingMessage!.PingValue);
    }
}
