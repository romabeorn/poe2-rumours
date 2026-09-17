using System.Text.Json;

namespace Rumours.Core;

public enum Tier
{
    SPlus, S, APlus, A, BPlus, B, C, D, F,
}

public static class TierText
{
    private static readonly Dictionary<string, Tier> ByLabel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S+"] = Tier.SPlus, ["S"] = Tier.S, ["A+"] = Tier.APlus, ["A"] = Tier.A,
        ["B+"] = Tier.BPlus, ["B"] = Tier.B, ["C"] = Tier.C, ["D"] = Tier.D, ["F"] = Tier.F,
    };

    public static Tier Parse(string label) =>
        ByLabel.TryGetValue(label.Trim(), out var tier) ? tier : throw new FormatException($"Unknown tier: {label}");

    private static readonly Dictionary<Tier, string> ByTier = ByLabel.ToDictionary(p => p.Value, p => p.Key);

    public static string Label(this Tier tier) => ByTier[tier];
}

public enum UiLanguage
{
    Russian,
    English,
}

public sealed record LocalText(string Russian, string English)
{
    public string In(UiLanguage language) => language == UiLanguage.English ? English : Russian;
}

public sealed record Rating(Tier Tier, LocalText Reward, LocalText? Note);

public sealed record Policy(int OpenAtGrandExpeditions, Tier OpenAtTier);

public sealed class RatingSheet
{
    private readonly Dictionary<string, Rating> _ratings;

    public Policy Policy { get; }

    private RatingSheet(Dictionary<string, Rating> ratings, Policy policy) => (_ratings, Policy) = (ratings, policy);

    public Rating For(Island island) => _ratings[island.Id];

    public static RatingSheet Parse(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
        var file = JsonSerializer.Deserialize<SheetFile>(json, options) ?? throw new FormatException("The ratings file is empty");

        var ratings = file.Islands.Where(p => IslandCatalog.Find(p.Key) is not null).ToDictionary(
            p => p.Key,
            p => new Rating(TierText.Parse(p.Value.Tier),
                Text(p.Value.Reward) ?? throw new FormatException($"Island {p.Key} has no reward"),
                Text(p.Value.Note)));

        var missing = IslandCatalog.All.Where(i => !ratings.ContainsKey(i.Id)).Select(i => i.Id).ToList();
        if (missing.Count > 0) throw new FormatException($"No ratings for: {string.Join(", ", missing)}");

        var policy = new Policy(file.Policy.OpenAtGrandExpeditions, TierText.Parse(file.Policy.OpenAtTier));
        return new RatingSheet(ratings, policy);
    }

    private sealed record SheetFile(PolicyEntry Policy, Dictionary<string, RatingEntry> Islands);
    private sealed record PolicyEntry(int OpenAtGrandExpeditions, string OpenAtTier);
    private static LocalText? Text(JsonElement? element) => element?.ValueKind switch
    {
        null or JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => new LocalText(element.Value.GetString()!, element.Value.GetString()!),
        JsonValueKind.Object => new LocalText(
            element.Value.GetProperty("ru").GetString()!, element.Value.GetProperty("en").GetString()!),
        _ => throw new FormatException("A rating text must be a string or a ru/en pair"),
    };

    private sealed record RatingEntry(string Tier, JsonElement? Reward, JsonElement? Note);
}
