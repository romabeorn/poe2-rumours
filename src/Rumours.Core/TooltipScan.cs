namespace Rumours.Core;

public sealed record TooltipScan(IReadOnlyList<Island> Recognised, int UnrecognisedLines, bool TooltipFound = true,
    bool LayoutKnown = true, GameLanguage Language = GameLanguage.English)
{
    public const int MaxShownRumours = 3;

    public static TooltipScan Miss { get; } = new([], 0, TooltipFound: false);

    public int Rows => Recognised.Count + UnrecognisedLines;

    public bool Complete => TooltipFound && LayoutKnown && Recognised.Count > 0 && UnrecognisedLines == 0;

    public static TooltipScan Combine(IEnumerable<TooltipScan> readings)
    {
        var found = readings.Where(r => r.TooltipFound).ToList();
        if (found.Count == 0) return Miss;

        var sameLanguage = found.GroupBy(r => r.Language)
            .OrderByDescending(g => g.Max(r => r.Recognised.Count)).First().ToList();

        if (sameLanguage.Where(r => r.Complete).MaxBy(r => r.Recognised.Count) is { } complete) return complete;

        var language = sameLanguage[0].Language;
        var seen = sameLanguage.SelectMany(r => r.Recognised).Distinct().ToList();
        if (seen.Count > MaxShownRumours)
        {
            var confirmed = seen.Where(i => sameLanguage.Count(r => r.Recognised.Contains(i)) > 1)
                .Take(MaxShownRumours - 1).ToList();
            return new TooltipScan(confirmed, MaxShownRumours - confirmed.Count, Language: language);
        }

        var rows = Math.Min(MaxShownRumours, sameLanguage.Max(r => r.Rows));
        return new TooltipScan(seen, Math.Max(0, rows - seen.Count),
            LayoutKnown: sameLanguage.Any(r => r.LayoutKnown), Language: language);
    }
}
