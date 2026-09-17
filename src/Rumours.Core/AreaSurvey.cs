namespace Rumours.Core;

public enum Verdict
{
    KeepScanning,
    Open,
    Skip,
}

public sealed class AreaSurvey(RatingSheet sheet)
{
    private readonly List<Island> _islands = [];

    public IReadOnlyList<Island> Islands => _islands.AsReadOnly();
    public int Scans { get; private set; }
    public int StaleScans { get; private set; }

    public bool FullSetSeen { get; private set; }

    public int GrandExpeditions => _islands.Count(i => i.Kind == IslandKind.GrandExpedition);

    public IReadOnlyList<Island> AddScan(TooltipScan scan)
    {
        if (scan.Recognised.Count == 0) return [];

        var fresh = scan.Recognised.Distinct().Where(i => !_islands.Contains(i)).ToList();
        _islands.AddRange(fresh);

        Scans++;
        if (fresh.Count > 0) StaleScans = 0;
        else if (scan.Complete) StaleScans++;
        if (scan.Complete && scan.Recognised.Count < TooltipScan.MaxShownRumours) FullSetSeen = true;

        return fresh;
    }

    public Verdict Verdict
    {
        get
        {
            if (_islands.Count == 0) return Verdict.KeepScanning;

            var best = _islands.Min(i => sheet.For(i).Tier);
            var worthOpening = GrandExpeditions >= sheet.Policy.OpenAtGrandExpeditions || best <= sheet.Policy.OpenAtTier;

            if (worthOpening) return Verdict.Open;
            return FullSetSeen ? Verdict.Skip : Verdict.KeepScanning;
        }
    }
}
