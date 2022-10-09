using MLL.Common.Files;

namespace MLL.Files.ImageLoader;

public static class ImageDataSetProviderExtensions
{
    private static readonly Dictionary<int, (string, object)> _cache;

    static ImageDataSetProviderExtensions() =>
        _cache = new Dictionary<int, (string, object)>();

    public static void EnsureKeys(int count)
    {
        for (int i = 0; i < count; i++)
            _cache[i] = (i.ToString(), i);
    }

    private static IEnumerable<(string, object)> IntToTuples(IEnumerable<int> indices) =>
        indices.Select(index => (index.ToString(), (object)index));

    public static int GetLargestImageDataSetCount(this IImageDataSetProvider provider, IEnumerable<int> indices) =>
        provider.GetLargestImageDataSetCount(IntToTuples(indices));

    public static void LoadAllImages(this IImageDataSetProvider provider, IEnumerable<int> indices) =>
        provider.LoadAllImages(IntToTuples(indices));

    public static IImageDataSet GetDataSet(this IImageDataSetProvider provider, int value)
    {
        if (!_cache.ContainsKey(value))
            _cache[value] = (value.ToString(), value);

        var (name, objValue) = _cache[value];
        return provider.GetDataSet(name, objValue);
    }
}
