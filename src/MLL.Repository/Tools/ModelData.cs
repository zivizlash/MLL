namespace MLL.Repository.Tools;

internal static class ModelData<T> where T : class
{
    public static Type Type { get; }
    public static string Name { get; }
    public static bool IsNameValid { get; }

    static ModelData()
    {
        Type = typeof(T);
        Name = Type.Name;
        IsNameValid = PathValidator.IsValidFileName(Name);
    }
}
