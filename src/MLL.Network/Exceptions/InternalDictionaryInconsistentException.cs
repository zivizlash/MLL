using System;

namespace MLL.Network.Exceptions;

public class InternalDictionaryInconsistentException : Exception
{
    public InternalDictionaryInconsistentException(string dictionaryName) : base(
        $"Exception due inconsistent internal {dictionaryName} dictionary state.")
    {
    }
}
