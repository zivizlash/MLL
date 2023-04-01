using MLL.Common.Files;
using MLL.Files.Tools;

namespace MLL.Files.ImageLoader;

public class ImageDataSet : IDataSet
{
    private readonly IFilesProvider _filesProvider;
    private readonly Dictionary<int, SetData> _indexToImage;

    public ImageDataSetOptions Options { get; }

    public float[] Value { get; }
    public int Count => _filesProvider.Count;

    public SetData this[int index] => GetOrLoad(index);

    public ImageDataSet(IFilesProvider filesProvider, float[] value, ImageDataSetOptions options)
    {
        Options = options;
        Value = value ?? throw new ArgumentNullException(nameof(value));

        _indexToImage = new Dictionary<int, SetData>();
        _filesProvider = filesProvider;
    }

    public void EnsureAllImagesLoaded()
    {
        for (int i = 0; i < Count; i++) GetOrLoad(i);
    }

    private SetData GetOrLoad(int index)
    {
        if (index >= Count || index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_indexToImage.TryGetValue(index, out var imageData))
            return imageData;

        using var imageStream = _filesProvider.OpenStream(index, out _);

        var imageValue = ImageTools.LoadImageData(imageStream, Options);
        return _indexToImage[index] = new ImageData(Value, imageValue);
    }
}
