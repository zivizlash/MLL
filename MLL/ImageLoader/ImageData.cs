namespace MLL.ImageLoader;

public class ImageData
{
    public object Value { get; }
    public float[] Data { get; }

    public ImageData(object value, float[] data)
    {
        Value = value;
        Data = data;
    }
}
