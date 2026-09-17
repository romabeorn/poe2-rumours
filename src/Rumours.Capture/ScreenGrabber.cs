using System.Drawing;
using System.Drawing.Imaging;

namespace Rumours.Capture;

public static class ScreenGrabber
{
    public static Bitmap Grab(Rectangle area)
    {
        var bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
        return bitmap;
    }
}
