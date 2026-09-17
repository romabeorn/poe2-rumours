using System.Drawing;
using Rumours.Core;

namespace Rumours.Capture;

public sealed class ScanReport(ScanOutcome outcome, Bitmap shot, Rectangle area) : IDisposable
{
    public ScanOutcome Outcome => outcome;
    public Bitmap Shot => shot;

    public Rectangle Area => area;

    public void Dispose() => shot.Dispose();
}

public sealed class TooltipScanner(IReadOnlyList<OcrReader> readers, ScanPlan plan)
{
    public static TooltipScanner ForGame() => new(OcrReader.ForGameLanguages(),
        new ScanPlan(new TooltipParser(new RumourMatcher(IslandCatalog.All)), new MapNameMatcher(IslandCatalog.All)));

    public IEnumerable<GameLanguage> Languages => readers.Select(r => r.Language);

    public Task<ScanReport> ScanAsync(Rectangle monitor, Point cursor)
    {
        var area = AroundCursor(monitor, cursor);
        return ReadAsync(ScreenGrabber.Grab(area), area);
    }

    public async Task<ScanReport> ReadAsync(Bitmap shot, Rectangle area)
    {
        try
        {
            var outcome = await plan.RunAsync(readers.Select(r => r.On(shot)).ToList(), GreyShot.From(shot));
            return new ScanReport(outcome, shot, area);
        }
        catch
        {
            shot.Dispose();
            throw;
        }
    }

    private static Rectangle AroundCursor(Rectangle monitor, Point cursor)
    {
        var side = monitor.Height;
        var area = new Rectangle(cursor.X - side / 2, cursor.Y - side / 2, side, side);
        area.Offset(
            Math.Max(0, monitor.Left - area.Left) - Math.Max(0, area.Right - monitor.Right),
            Math.Max(0, monitor.Top - area.Top) - Math.Max(0, area.Bottom - monitor.Bottom));
        return Rectangle.Intersect(area, monitor);
    }
}
