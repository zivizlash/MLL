using MLL.Network.Message.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLL.Race.Web.Common.Messages;

public class MessageTypesProvider : IMessageTypesProvider
{
    private Type[]? _cache;

    public IEnumerable<Type> GetTypes()
    {
        return _cache ??= GetTypesInternal().ToArray();
    }

    private IEnumerable<Type> GetTypesInternal()
    {
        var messagesNamespaceName = string.Join('.', new string[]
        {
            nameof(MLL), nameof(Race), nameof(Web), nameof(Common), nameof(Messages)
        });

        var innerNamespacesPattern = messagesNamespaceName + ".";

        var types = typeof(MessageTypesProvider).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace.StartsWith(innerNamespacesPattern));

        return types;
    }
}
