using System.Drawing;
using Rumours.Capture;
using Rumours.Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;
if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: OcrProbe <file or folder with screenshots> [--lines]");
    return 1;
}

var showLines = args.Contains("--lines");
string[] extensions = [".png", ".jpg", ".jpeg"];
var files = Directory.Exists(args[0])
    ? Directory.EnumerateFiles(args[0]).Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).Order().ToList()
    : [args[0]];

var scanner = TooltipScanner.ForGame();
var english = GameLanguage.English;
Console.WriteLine($"Recognisers: {string.Join(", ", scanner.Languages)}");

var clean = 0;
foreach (var file in files)
{
    var bitmap = new Bitmap(file);
    var watch = System.Diagnostics.Stopwatch.StartNew();
    using var report = await scanner.ReadAsync(bitmap, new Rectangle(Point.Empty, bitmap.Size));
    if (report.Outcome.Clean) clean++;

    Console.WriteLine($"\n{Path.GetFileName(file)} — {bitmap.Width}×{bitmap.Height}, {watch.ElapsedMilliseconds} ms, passes: {report.Outcome.Readings.Count}");
    foreach (var reading in report.Outcome.Readings)
    {
        Console.WriteLine($"   pass \"{reading.Language}, {reading.Preprocessing.Id}\": rumours {reading.Scan.Recognised.Count}, unread {reading.Scan.UnrecognisedLines}"
            + (reading.Scan.TooltipFound ? "" : ", tooltip not found"));
        if (showLines)
            foreach (var line in reading.Lines) Console.WriteLine($"      [{line.X,5:0},{line.Y,5:0}] {line.Text}");
    }

    if (report.Outcome.Map is { } map) Console.WriteLine($"   ✔ map under the cursor: {map.Island.In(english).Map}");
    else if (!report.Outcome.Scan.TooltipFound) Console.WriteLine("   ✘ tooltip not found");
    foreach (var island in report.Outcome.Scan.Recognised) Console.WriteLine($"   ✔ {island.In(english).Rumour} → {island.In(english).Map}");
    if (!report.Outcome.Clean && report.Outcome.Scan.TooltipFound) Console.WriteLine($"   ✘ incomplete: unread rows {report.Outcome.Scan.UnrecognisedLines}, layout {(report.Outcome.Scan.LayoutKnown ? "known" : "unknown")}");
}

Console.WriteLine($"\nRead cleanly: {clean} of {files.Count}");
return 0;
