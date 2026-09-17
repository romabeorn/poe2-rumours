namespace Rumours.Core;

public sealed class GreyImage
{
    private const double InkBelowPaper = 0.4;
    private const double InkShareOfWrittenSlot = 0.015;
    private const int DarkestParchment = 70;

    private readonly byte[] _luma;

    public int Width { get; }
    public int Height { get; }

    public GreyImage(int width, int height, byte[] luma)
    {
        if (luma.Length != width * height) throw new ArgumentException("The buffer length does not match the image size");
        (Width, Height, _luma) = (width, height, luma);
    }

    public bool HasInk(double x, double y, double width, double height)
    {
        int left = Math.Max(0, (int)x), top = Math.Max(0, (int)y);
        int right = Math.Min(Width, (int)(x + width)), bottom = Math.Min(Height, (int)(y + height));
        if (right <= left || bottom <= top) return false;

        var histogram = new int[256];
        for (var row = top; row < bottom; row++)
            for (var column = left; column < right; column++) histogram[_luma[row * Width + column]]++;

        var area = (right - left) * (bottom - top);
        var paper = Median(histogram, area);
        if (paper < DarkestParchment) return false;

        var ink = histogram.Take((int)(paper * InkBelowPaper)).Sum();
        return ink >= area * InkShareOfWrittenSlot;
    }

    private static int Median(int[] histogram, int area)
    {
        for (int level = 0, seen = 0; level < 256; level++)
            if ((seen += histogram[level]) > area / 2) return level;
        return 255;
    }
}
