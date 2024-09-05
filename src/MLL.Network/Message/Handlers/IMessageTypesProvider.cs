using System;
using System.Collections.Generic;

namespace MLL.Network.Message.Handlers;

public class MessageTypesProvider : IMessageTypesProvider
{
    private readonly Type[] _types;

    public MessageTypesProvider(params Type[] types)
    {
        _types = types ?? throw new ArgumentNullException(nameof(types));
    }

    public IEnumerable<Type> GetTypes()
    {
        return _types;
    }
}

public interface IMessageTypesProvider
{
    IEnumerable<Type> GetTypes();
}
