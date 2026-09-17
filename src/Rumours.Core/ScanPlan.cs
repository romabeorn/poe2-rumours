namespace Rumours.Core;

public sealed record Preprocessing(string Id, double Upscale, bool HighContrastGrey);

public interface IShotReader
{
    GameLanguage Language { get; }

    Task<IReadOnlyList<TextLine>> ReadAsync(Preprocessing preprocessing);
}

public sealed record Reading(GameLanguage Language, Preprocessing Preprocessing, IReadOnlyList<TextLine> Lines, TooltipScan Scan);

public sealed record ScanOutcome(TooltipScan Scan, (Island Island, GameLanguage Language)? Map, IReadOnlyList<Reading> Readings)
{
    public bool Clean => Map is not null || Scan.Complete;
}

public sealed class ScanPlan(TooltipParser parser, MapNameMatcher maps)
{
    private static readonly Preprocessing[] Passes =
    [
        new("colour ×2", 2, HighContrastGrey: false),
        new("contrast ×3", 3, HighContrastGrey: true),
        new("colour ×3", 3, HighContrastGrey: false),
    ];

    private GameLanguage? _lastSuccessful;

    public async Task<ScanOutcome> RunAsync(IReadOnlyList<IShotReader> readers, GreyImage shot)
    {
        var all = new List<Reading>();
        foreach (var reader in readers.OrderByDescending(r => r.Language == _lastSuccessful))
        {
            var own = new List<Reading>();
            foreach (var pass in Passes)
            {
                var lines = await reader.ReadAsync(pass);
                own.Add(new Reading(reader.Language, pass, lines, parser.Parse(lines, shot)));

                var soFar = TooltipScan.Combine(own.Select(r => r.Scan));
                if (soFar.Complete || !soFar.TooltipFound) break;
            }
            all.AddRange(own);

            var scan = TooltipScan.Combine(own.Select(r => r.Scan));
            var map = scan.TooltipFound ? null : own.Select(r => maps.Match(r.Lines)).FirstOrDefault(m => m is not null);
            if (!scan.TooltipFound && map is null) continue;

            _lastSuccessful = reader.Language;
            return new ScanOutcome(scan, map, all);
        }
        return new ScanOutcome(TooltipScan.Miss, null, all);
    }
}
