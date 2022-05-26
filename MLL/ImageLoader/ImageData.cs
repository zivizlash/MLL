namespace MLL.ImageLoader;

public class ImageData
{
    public object Value { get; }
    public double[] Data { get; }

    public ImageData(object value, double[] data)
    {
        Value = value;
        Data = data;
    }
}
