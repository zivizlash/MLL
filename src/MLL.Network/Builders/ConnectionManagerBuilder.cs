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
        IBuilder WithUsedTypes(IMessageTypesProvider typesProvider);
        IBuilder WithUsedTypes(params Type[] types);
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

    IBuilder IUsedTypesBuilder.WithUsedTypes(IMessageTypesProvider typesProvider)
    {
        _typesProvider = typesProvider ?? throw new ArgumentNullException(nameof(typesProvider));
        return this;
    }

    IBuilder IUsedTypesBuilder.WithUsedTypes(params Type[] types)
    {
        _typesProvider = new MessageTypesProvider(types);
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
        var pipeFactory = CreatePipeFactory();
        var listener = new ServerListenerToMessageHandler(pipeFactory);
        return new ServerConnectionAcceptor(_endpoint!, listener);
    }

    private ListenerMessageHandlerPipeFactory CreatePipeFactory()
    {
        var acceptableTypes = _typesProvider!.GetTypes();

        var hashCode = new ProtocolVersionHashCode();
        var messageConverter = new MessageConverter(acceptableTypes, hashCode);
        var attributeMessageHandlerBinder = new AttributeMessageHandlerBinder();

        return new ListenerMessageHandlerPipeFactory(
            messageConverter, _handlerFactory!, attributeMessageHandlerBinder);
    }

    private void CheckNulls()
    {
        _ = _endpoint ?? throw new NullReferenceException();
        _ = _handlerFactory ?? throw new NullReferenceException();
        _ = _typesProvider ?? throw new NullReferenceException();
    }
}
