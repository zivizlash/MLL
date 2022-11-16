using System;
using System.Collections.Generic;

namespace MLL.Network.Message.Protocol;

public class HashCodeComparer : IComparer<Type>
{
    private readonly ProtocolVersionHashCode _hashCode;

    public HashCodeComparer(ProtocolVersionHashCode hashCode)
    {
        _hashCode = hashCode;
    }

    public int Compare(Type x, Type y)
    {
        var c1 = _hashCode.CalculateByAcceptableTypes(new List<Type> { x });
        var c2 = _hashCode.CalculateByAcceptableTypes(new List<Type> { y });

        return c1.CompareTo(c2);
    }
}
