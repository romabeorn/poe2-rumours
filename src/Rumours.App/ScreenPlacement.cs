using System.Windows;

namespace Rumours.App;

internal static class ScreenPlacement
{
    private const double Reachable = 120;

    public static (double Left, double Top) Visible(Point remembered)
    {
        var left = Math.Clamp(remembered.X, SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Reachable);
        var top = Math.Clamp(remembered.Y, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Reachable);
        return (left, top);
    }
}
