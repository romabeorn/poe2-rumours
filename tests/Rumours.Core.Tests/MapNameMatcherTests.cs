namespace Rumours.Core.Tests;

public class MapNameMatcherTests
{
    private readonly MapNameMatcher _matcher = new(IslandCatalog.All);

    private static TextLine[] Lines(params string[] texts) =>
        texts.Select((t, i) => new TextLine(t, 600, 900 + i * 40, 240, 24)).ToArray();

    [Theory]
    [InlineData("SPRAWLING JUNGLE", "sprawling-jungle")]
    [InlineData("SPRAWUNG JUNGLE", "sprawling-jungle")]
    public void Finds_the_map_among_the_tooltip_lines(string title, string expectedId)
    {
        var match = _matcher.Match(Lines("WORLD", "ACT 1", title, "BIOME: OCEAN", "WHAT ONCE WAS, SHALL BE", "AGAIN.",
            "REQUIRES: WAYSTONE (TIER 11) OR HIGHER"));

        Assert.Equal(expectedId, match?.Island.Id);
        Assert.Equal(GameLanguage.English, match?.Language);
    }

    [Fact]
    public void Knows_every_map_in_both_languages()
    {
        foreach (var island in IslandCatalog.All)
            foreach (var (language, text) in island.Texts)
                Assert.Equal((island, language), _matcher.Match(Lines(text.Map.ToUpperInvariant())));
    }

    [Fact]
    public void Ordinary_screen_text_is_not_a_map() =>
        Assert.Null(_matcher.Match(Lines("WORLD", "INVENTORY", "Search here", "ENDGAME", "Hidden Grotto", "BIOME: OCEAN")));
}
