using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rumours.Core;

namespace Rumours.App;

internal static class OverlayPreview
{
    public static bool TryRun(string[] args, RatingSheet sheet, Settings settings, UiText text)
    {
        if (args is not [var command, var file, .. var language]) return false;
        if (language is [var forced]) text = UiText.For(forced);

        switch (command)
        {
            case "--preview": Render(sheet, settings, text, file); return true;
            case "--preview-menu": RenderMenu(sheet, settings, text, file, onSettingsTab: false); return true;
            case "--preview-settings": RenderMenu(sheet, settings, text, file, onSettingsTab: true); return true;
            default: return false;
        }
    }

    private static void Render(RatingSheet sheet, Settings settings, UiText text, string file)
    {
        var viewModel = new OverlayViewModel(sheet, settings, text);
        viewModel.Apply(Scan("bleached-shoals", "craggy-peninsula", "moment-of-zen"));
        viewModel.Apply(Scan("craggy-peninsula", "exhumed-ruins", "secluded-temple"));
        viewModel.Apply(Scan("exhumed-ruins", "craggy-peninsula", "moment-of-zen"));
        viewModel.Apply(Scan("moor", "craggy-peninsula", "exhumed-ruins"));

        Save(new OverlayWindow(viewModel, settings), file);
    }

    private static void RenderMenu(RatingSheet sheet, Settings settings, UiText text, string file, bool onSettingsTab)
    {
        var viewModel = new MenuViewModel(text, new CatalogueViewModel(sheet, text), settings.Hotkeys, settings.Appearance);
        viewModel.ShowSettings(onSettingsTab);
        Save(new MenuWindow(viewModel), file);
    }

    private static void Save(Window window, string file)
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.ShowActivated = false;
        (window.Left, window.Top) = (-20000, -20000);
        window.Show();
        window.UpdateLayout();

        var root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        window.Close();

        var encoder = new PngBitmapEncoder { Frames = { BitmapFrame.Create(bitmap) } };
        using var stream = File.Create(file);
        encoder.Save(stream);
    }

    private static TooltipScan Scan(params string[] ids) => new(ids.Select(IslandCatalog.ById).ToList(), 0);
}
