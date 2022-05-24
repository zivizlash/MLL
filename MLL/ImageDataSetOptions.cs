namespace MLL;

public readonly struct ImageDataSetOptions
{
    public int Width { get; }
    public int Height { get; }

    public static ImageDataSetOptions Default => new ImageDataSetOptions(128, 128);

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
