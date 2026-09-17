using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Rumours.Capture;
using Rumours.Core;

namespace Rumours.App;

public sealed class ScanLog(string folder, ShotSaving saveShots)
{
    private const int KeptShots = 40;

    public string Folder => folder;

    public void Write(ScanReport report)
    {
        try
        {
            Directory.CreateDirectory(folder);
            var stamp = DateTime.Now;
            string? shotName = null;

            if (saveShots == ShotSaving.All || (saveShots == ShotSaving.Failed && !report.Outcome.Clean))
            {
                shotName = $"scan-{stamp:yyyyMMdd-HHmmss-fff}.png";
                report.Shot.Save(Path.Combine(folder, shotName), ImageFormat.Png);
                Prune();
            }

            File.AppendAllText(Path.Combine(folder, "scans.log"), Describe(report, stamp, shotName), Encoding.UTF8);
        }
        catch (Exception)
        {
        }
    }

    public void WriteError(Exception error)
    {
        try
        {
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "errors.log"), $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{error}\n\n", Encoding.UTF8);
        }
        catch (Exception)
        {
        }
    }

    private static string Describe(ScanReport report, DateTime stamp, string? shotName)
    {
        var scan = report.Outcome.Scan;
        var english = GameLanguage.English;
        var outcome = report.Outcome.Map is { } map ? $"MAP: {map.Island.In(english).Map}"
            : !scan.TooltipFound ? "TOOLTIP NOT FOUND"
            : report.Outcome.Clean ? "ok"
            : $"UNREAD ROWS: {scan.UnrecognisedLines}";

        var text = new StringBuilder()
            .AppendLine($"=== {stamp:yyyy-MM-dd HH:mm:ss} · {outcome} · area {report.Area.Width}×{report.Area.Height} at ({report.Area.X},{report.Area.Y})"
                + (shotName is null ? "" : $" · shot {shotName}"))
            .AppendLine($"    result: {string.Join(" | ", scan.Recognised.Select(i => i.In(english).Rumour))}");

        foreach (var reading in report.Outcome.Readings)
        {
            text.AppendLine($"  -- pass \"{reading.Language}, {reading.Preprocessing.Id}\": tooltip {(reading.Scan.TooltipFound ? "found" : "not found")}, "
                + $"rumours {reading.Scan.Recognised.Count}, unread {reading.Scan.UnrecognisedLines}");
            foreach (var line in reading.Lines)
                text.AppendLine($"     [{line.X,5:0},{line.Y,5:0} {line.Width,4:0}×{line.Height,-3:0}] {line.Text}");
        }
        return text.AppendLine().ToString();
    }

    private void Prune()
    {
        foreach (var old in Directory.EnumerateFiles(folder, "scan-*.png").OrderDescending().Skip(KeptShots))
            File.Delete(old);
    }
}
