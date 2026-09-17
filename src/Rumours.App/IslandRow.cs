using System.Windows;
using System.Windows.Media;
using Rumours.Core;

namespace Rumours.App;

public sealed record IslandRow(string Tier, Brush TierBrush, string Rumour, string Map, string Kind, Brush KindBrush,
    string Reward, string? Note, string NewMark)
{
    private static Brush Top => Palette.Theme("TierTopBrush");
    private static Brush Good => Palette.Theme("TierGoodBrush");
    private static Brush Fair => Palette.Theme("TierFairBrush");
    private static Brush Bad => Palette.Theme("TierBadBrush");
    private static Brush Plain => Palette.Theme("TextBrush");
    private static Brush Muted => Palette.Theme("LabelBrush");
    private static Brush Unique => Palette.Theme("FlavourBrush");

    public static IslandRow For(Island island, RatingSheet sheet, UiText text, GameLanguage gameLanguage, bool isNew = false)
    {
        var rating = sheet.For(island);
        var names = island.In(gameLanguage);
        var (kind, kindBrush) = island.Kind switch
        {
            IslandKind.GrandExpedition => (text.KindExpedition, Plain),
            IslandKind.Boss => (text.KindBoss, Bad),
            _ => (text.KindUnique, Unique),
        };
        var tierBrush = rating.Tier switch
        {
            <= Core.Tier.S => Top,
            <= Core.Tier.A => Good,
            <= Core.Tier.B => Fair,
            Core.Tier.C => Muted,
            _ => Bad,
        };
        return new IslandRow(rating.Tier.Label(), tierBrush, names.Rumour, names.Map, kind, kindBrush,
            rating.Reward.In(text.Language), rating.Note?.In(text.Language), isNew ? text.NewMark : "");
    }
}

public sealed record IslandTableModel(UiText Text, IReadOnlyList<IslandRow> Rows)
{
    public bool HasRows => Rows.Count > 0;
}

internal static class Palette
{
    public static Brush Theme(string key) => Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
}
