namespace MLL.Common.Files;

public interface IImageDataSet
{
    public object Value { get; }
    public int Count { get; }
    ImageDataSetOptions Options { get; }
    ImageData this[int index] { get; }
    void EnsureAllImagesLoaded();
}
