using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers.Binding;

public partial class AttributeMessageHandlerBinder
{
    private readonly Type[] _validReturnTypes;
    private readonly Dictionary<Type, MessageHandlerFactoryInfo[]> _factories;
    private readonly MessageHandlerDelegateFactoryCreator _factoryCreator;

    public AttributeMessageHandlerBinder()
    {
        _validReturnTypes = new[] { typeof(void), typeof(Task), typeof(ValueTask) };
        _factories = new();
        _factoryCreator = new();
    }

    public IMultiMessageHandler Bind(Type type, object target)
    {
        if (!_factories.TryGetValue(type, out var factories))
        {
            _factories[type] = factories = GenerateHandlerFactories(type);
        }

        var typeToHandler = new Dictionary<Type, IMessageHandler>(factories.Length);

        foreach (var factory in factories)
        {
            var handler = factory.Factory.Invoke(target);
            typeToHandler.Add(factory.MessageType, handler);
        }

        return new BinderMultiMessageHandler(typeToHandler);
    }

    private MessageHandlerFactoryInfo[] GenerateHandlerFactories(Type targetType)
    {
        var factories = new List<MessageHandlerFactoryInfo>();

        foreach (var method in targetType.GetMethods().Where(IsHandler))
        {
            var parameters = method.GetParameters();

            if (parameters.Length != 1)
            {
                throw new InvalidOperationException(
                    "Handlers require 1 parameter which is the message.");
            }

            if (!_validReturnTypes.Contains(method.ReturnType))
            {
                throw new InvalidOperationException(
                    "Handlers required void, Task or ValueTask type as return parameter");
            }

            var messageType = parameters.Single().ParameterType;
            Delegate outputDelegate;

            if (method.ReturnType == typeof(void))
            {
                var pattern = typeof(Action<,>).MakeGenericType(targetType, messageType);
                outputDelegate = method.CreateDelegate(pattern);
            }
            else
            {
                var pattern = typeof(Func<,,>).MakeGenericType(targetType, messageType, method.ReturnType);
                outputDelegate = method.CreateDelegate(pattern);
            }

            var funcFactory = _factoryCreator.CreateFactory(targetType, messageType, outputDelegate);
            factories.Add(new MessageHandlerFactoryInfo(funcFactory, messageType));
        }

        return factories.ToArray();
    }

    private static bool IsHandler(MethodInfo method)
    {
        return method.GetCustomAttribute<MessageHandlerAttribute>() != null;
    }

    private readonly struct MessageHandlerFactoryInfo
    {
        public readonly Func<object, IMessageHandler> Factory;
        public readonly Type MessageType;

        public MessageHandlerFactoryInfo(Func<object, IMessageHandler> factory, Type messageType)
        {
            Factory = factory;
            MessageType = messageType;
        }
    }
}
