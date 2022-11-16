using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MLL.Network.Message.Protocol;

public class ProtocolVersionHashCode
{
    private IEnumerable<string> GetHashCodeStrings(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

        if (type.IsArray)
        {
            yield return type.GetArrayRank().ToString();

            foreach (var inner in GetHashCodeStrings(type.GetElementType()))
            {
                yield return inner;
            }

            yield break;
        }

        foreach (var property in type.GetProperties(flags))
        {
            yield return property.ReflectedType.Name;
            yield return property.SetMethod?.Name ?? string.Empty;
            yield return property.GetMethod?.Name ?? string.Empty;
        }

        foreach (var field in type.GetFields(flags))
        {
            yield return field.ReflectedType.Name;
            yield return field.Name;
        }

        foreach (var inner in type.GenericTypeArguments.SelectMany(GetHashCodeStrings))
        {
            yield return inner;
        }
    }

    public int CalculateByAcceptableTypes(IEnumerable<Type> types)
    {
        int hashCode = 0;

        foreach (var typeString in types.SelectMany(GetHashCodeStrings))
        {
            hashCode <<= 2;
            hashCode += typeString.Sum(ch => ch);
            hashCode *= 7;
        }

        return hashCode;
    }
}
