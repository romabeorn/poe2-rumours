using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Rumours.App;

internal static class ThemeTextures
{
    public static void Apply(ResourceDictionary resources, string folder)
    {
        var file = Path.Combine(folder, "panel.png");
        if (!File.Exists(file)) return;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(file);
        image.EndInit();
        image.Freeze();

        resources["PanelImage"] = image;
    }
}
