using Microsoft.Extensions.Logging;
using MLL.Common.Pooling;
using MLL.Network.Factories;
using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Handlers.Binding;
using MLL.Network.Message.Listening;
using MLL.Network.Message.Protocol;
using System;
using System.Net;

namespace MLL.Network.Builders;

public class ConnectionManagerBuilder : 
    ConnectionManagerBuilder.IEndpointBuilder,
    ConnectionManagerBuilder.IFactoryBuilder,
    ConnectionManagerBuilder.IUsedTypesBuilder,
    ConnectionManagerBuilder.ILoggerFactoryBuilder,
    ConnectionManagerBuilder.IBuilder
{
    #region Interfaces
    public interface IEndpointBuilder
    {
        IFactoryBuilder WithAddress(IPEndPoint endpoint);
    }

    public interface IFactoryBuilder
    {
        IUsedTypesBuilder WithHandlerFactory(IMessageHandlerFactory factory);
    }

    public interface IUsedTypesBuilder
    {
        ILoggerFactoryBuilder WithUsedTypes(IMessageTypesProvider typesProvider);
        ILoggerFactoryBuilder WithUsedTypes(params Type[] types);
    }

    public interface ILoggerFactoryBuilder
    {
        IBuilder WithLoggerFactory(ILoggerFactory loggerFactory);
    }

    public interface IBuilder
    {
        ServerConnectionAcceptor BuildServer();
        ClientConnectionAcceptor BuildClient();
    }
    #endregion

    private IPEndPoint? _endpoint;
    private IMessageHandlerFactory? _handlerFactory;
    private IMessageTypesProvider? _typesProvider;
    private ILoggerFactory? _loggerFactory;

    public IFactoryBuilder WithAddress(IPEndPoint endpoint)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        return this;
    }

    IUsedTypesBuilder IFactoryBuilder.WithHandlerFactory(IMessageHandlerFactory factory)
    {
        _handlerFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    ILoggerFactoryBuilder IUsedTypesBuilder.WithUsedTypes(IMessageTypesProvider typesProvider)
    {
        _typesProvider = typesProvider ?? throw new ArgumentNullException(nameof(typesProvider));
        return this;
    }

    ILoggerFactoryBuilder IUsedTypesBuilder.WithUsedTypes(params Type[] types)
    {
        _typesProvider = new MessageTypesProvider(types);
        return this;
    }

    IBuilder ILoggerFactoryBuilder.WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        return this;
    }

    ClientConnectionAcceptor IBuilder.BuildClient()
    {
        CheckNulls();
        var pipeFactory = CreatePipeFactory();
        var listener = new ClientListenerToMessageHandler(pipeFactory);
        return new ClientConnectionAcceptor(_endpoint!, listener);
    }

    ServerConnectionAcceptor IBuilder.BuildServer()
    {
        CheckNulls();
        var logger = _loggerFactory.CreateLogger<ServerConnectionAcceptor>();
        var listener = new ServerListenerToMessageHandler(CreatePipeFactory());

        return new ServerConnectionAcceptor(_endpoint!, listener, logger, tcpClient => 
        {
            tcpClient.ReceiveTimeout = 5000;
            tcpClient.SendTimeout = 5000;
        });
    }

    private ListenerMessageHandlerPipeFactory CreatePipeFactory()
    {
        var acceptableTypes = _typesProvider!.GetTypes();

        var hashCode = new ProtocolVersionHashCode();
        var messageConverter = new MessageConverter(acceptableTypes, hashCode);
        var attributeMessageHandlerBinder = new AttributeMessageHandlerBinder();

        var dataPool = new CollectionPool<byte>(2048);
        var internalPool = new CollectionPool<byte>(4);

        return new ListenerMessageHandlerPipeFactory(messageConverter, _handlerFactory!, 
            attributeMessageHandlerBinder, _loggerFactory!, dataPool, internalPool);
    }

    private void CheckNulls()
    {
        _ = _endpoint ?? throw new NullReferenceException();
        _ = _handlerFactory ?? throw new NullReferenceException();
        _ = _typesProvider ?? throw new NullReferenceException();
        _ = _loggerFactory ?? throw new NullReferenceException();
    }
}
