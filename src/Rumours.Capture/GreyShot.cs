using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Rumours.Core;

namespace Rumours.Capture;

public static class GreyShot
{
    public static GreyImage From(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[data.Stride];
            var luma = new byte[data.Width * data.Height];
            for (var y = 0; y < data.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                for (var x = 0; x < data.Width; x++)
                {
                    var i = x * 4;
                    luma[y * data.Width + x] = (byte)((row[i] * 29 + row[i + 1] * 150 + row[i + 2] * 77) >> 8);
                }
            }
            return new GreyImage(data.Width, data.Height, luma);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
