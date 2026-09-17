namespace Rumours.Core;

public sealed record TextLine(string Text, double X, double Y, double Width, double Height)
{
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
    public double Bottom => Y + Height;
}

public sealed class TooltipParser(RumourMatcher matcher)
{
    private const double AnchorSimilarity = 0.7;

    private static readonly double[] SlotCentres = [0.305, 0.54, 0.795];
    private const double SlotHalfHeight = 0.09;

    private const double IntroToHeader = 0.164, HeaderToIntroWidth = 0.56;

    private const double InkProbeWidthInHeaders = 0.9;

    public TooltipScan Parse(IReadOnlyList<TextLine> lines, GreyImage? shot = null)
    {
        foreach (var locale in GameLocales.All)
        {
            var scan = Parse(lines, shot, locale);
            if (scan.TooltipFound) return scan;
        }
        return TooltipScan.Miss;
    }

    private TooltipScan Parse(IReadOnlyList<TextLine> lines, GreyImage? shot, GameLocale locale)
    {
        var header = lines.FirstOrDefault(l => Resembles(l.Text, locale.Header));
        var anchor = header ?? lines.FirstOrDefault(l => Resembles(l.Text, locale.Intro));
        if (anchor is null) return TooltipScan.Miss;

        var below = lines
            .Where(l => l.Y > anchor.CenterY && Math.Abs(l.CenterX - anchor.CenterX) < anchor.Width)
            .OrderBy(l => l.Y)
            .ToList();
        var footer = below.FirstOrDefault(l => Resembles(l.Text, locale.Footer));

        return footer is null
            ? ReadWithoutLayout(below, locale)
            : ReadSlots(below.TakeWhile(l => l != footer).ToList(), header, anchor, footer, shot, locale);
    }

    private TooltipScan ReadSlots(IReadOnlyList<TextLine> body, TextLine? header, TextLine anchor, TextLine footer,
        GreyImage? shot, GameLocale locale)
    {
        var top = header?.Y ?? anchor.Y + IntroToHeader * (footer.Y - anchor.Y);
        var headerWidth = header?.Width ?? anchor.Width * HeaderToIntroWidth;
        var span = footer.Y - top;

        var recognised = new List<Island>();
        var unrecognised = 0;
        var placed = new HashSet<TextLine>();

        foreach (var centre in SlotCentres.Select(share => top + share * span))
        {
            var pieces = body.Where(l => Math.Abs(l.CenterY - centre) < SlotHalfHeight * span).OrderBy(l => l.X).ToList();
            placed.UnionWith(pieces);

            if (pieces.Count > 0)
            {
                if (matcher.Match(string.Join(' ', pieces.Select(l => l.Text))) is { } island) recognised.Add(island);
                else unrecognised++;
            }
            else if (shot?.HasInk(anchor.CenterX - headerWidth * InkProbeWidthInHeaders / 2, centre - SlotHalfHeight * span,
                         headerWidth * InkProbeWidthInHeaders, 2 * SlotHalfHeight * span) == true)
            {
                unrecognised++;
            }
        }

        var layoutBroken = body.Any(l => !placed.Contains(l) && matcher.Match(l.Text) is not null);

        recognised = recognised.Distinct().ToList();
        return new TooltipScan(recognised, unrecognised, LayoutKnown: !layoutBroken, Language: locale.Language);
    }

    private TooltipScan ReadWithoutLayout(IReadOnlyList<TextLine> below, GameLocale locale)
    {
        var recognised = below.Take(TooltipScan.MaxShownRumours + 2)
            .Select(l => matcher.Match(l.Text)).OfType<Island>().Distinct()
            .Take(TooltipScan.MaxShownRumours).ToList();
        return new TooltipScan(recognised, 0, LayoutKnown: false, Language: locale.Language);
    }

    private static bool Resembles(string text, string key) =>
        RumourMatcher.Similarity(RumourMatcher.Normalize(text), key) >= AnchorSimilarity;
}
