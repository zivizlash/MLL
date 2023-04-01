using System.Collections.Immutable;

namespace MLL.Repository.Tools;

internal static class PathValidator
{
    private static readonly ImmutableHashSet<char> _invalidChars;

    public static bool IsValidFileName(string fileName)
    {
        return !fileName.Any(_invalidChars.Contains);
    }

    static PathValidator()
    {
        _invalidChars = Path.GetInvalidFileNameChars().ToImmutableHashSet();
    }
}
