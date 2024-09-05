namespace MLL.Common.Files;

public readonly struct ImageDataSetOptions
{
    public int Width { get; }
    public int Height { get; }
    
    public static readonly ImageDataSetOptions Default = new(64, 64);

    public ImageDataSetOptions(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(width));

        Width = width;
        Height = height;
    }

    public override int GetHashCode() => 
        HashCode.Combine(Width, Height);

    public override bool Equals(object? obj) =>
        obj is ImageDataSetOptions other 
        && Width == other.Width && Height == other.Height;
}
