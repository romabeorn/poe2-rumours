namespace Rumours.Core.Tests;

public class ScanPlanTests
{
    private static readonly GreyImage Blank = new(1, 1, [0]);

    private static ScanPlan Plan() =>
        new(new TooltipParser(new RumourMatcher(IslandCatalog.All)), new MapNameMatcher(IslandCatalog.All));

    private sealed class FakeReader(GameLanguage language, params string[][] readings) : IShotReader
    {
        public int Reads { get; private set; }
        public GameLanguage Language => language;

        public Task<IReadOnlyList<TextLine>> ReadAsync(Preprocessing preprocessing)
        {
            var texts = readings[Math.Min(Reads++, readings.Length - 1)];
            return Task.FromResult<IReadOnlyList<TextLine>>(Layout(texts));
        }
    }

    private static List<TextLine> Layout(string[] texts)
    {
        double[] rows = [393, 437, 480, 523, 572];
        return texts.Select((t, i) => new TextLine(t, 650, rows[i], 150, 24)).Where(l => l.Text != "").ToList();
    }

    private static string[] English(params string[] rumours) =>
        ["Island Rumours", .. rumours.Concat(Enumerable.Repeat("", 3)).Take(3), "Requires:"];

    private static string[] Russian(params string[] rumours) =>
        ["Слухи об острове", .. rumours.Concat(Enumerable.Repeat("", 3)).Take(3), "Требует:"];

    [Fact]
    public async Task Stops_after_the_first_complete_reading()
    {
        var reader = new FakeReader(GameLanguage.English, English("Sulphite!", "Cold as ice...", "Fallen stars..."));
        var outcome = await Plan().RunAsync([reader], Blank);

        Assert.True(outcome.Clean);
        Assert.Equal(1, reader.Reads);
    }

    [Fact]
    public async Task Tries_other_preprocessing_until_the_gaps_are_covered()
    {
        var reader = new FakeReader(GameLanguage.English,
            English("Sulphite!", "~~~ ~~~", "Fallen stars..."),
            English("~~~ ~~~", "Cold as ice...", "Fallen stars..."));
        var outcome = await Plan().RunAsync([reader], Blank);

        Assert.Equal(2, reader.Reads);
        Assert.Equal(3, outcome.Scan.Recognised.Count);
        Assert.True(outcome.Clean);
    }

    [Fact]
    public async Task A_reader_of_the_wrong_alphabet_is_dropped_after_one_pass()
    {
        var english = new FakeReader(GameLanguage.English, ["Cnyxu 06 ocmpoBe", "CnaBHbiu manbiu", "", "", "Tpe6yem:"]);
        var russian = new FakeReader(GameLanguage.Russian, Russian("Славный малый...", "Сульфит!", "Почти рай."));
        var outcome = await Plan().RunAsync([english, russian], Blank);

        Assert.Equal(1, english.Reads);
        Assert.Equal(GameLanguage.Russian, outcome.Scan.Language);
        Assert.True(outcome.Clean);
    }

    [Fact]
    public async Task Remembers_which_language_worked_last_time()
    {
        var plan = Plan();
        var english = new FakeReader(GameLanguage.English, ["mush"]);
        var russian = new FakeReader(GameLanguage.Russian, Russian("Сульфит!"));
        await plan.RunAsync([english, russian], Blank);
        await plan.RunAsync([english, russian], Blank);

        Assert.Equal(1, english.Reads);
        Assert.Equal(2, russian.Reads);
    }

    [Fact]
    public async Task The_verdict_on_a_reader_uses_all_its_readings_not_the_last_one()
    {
        var english = new FakeReader(GameLanguage.English,
            English("Sulphite!", "~~~ ~~~", "~~~ ~~~"),
            English("Sulphite!", "~~~ ~~~", "~~~ ~~~"),
            ["~~~", "~~~", "~~~", "~~~", "~~~"]);
        var russian = new FakeReader(GameLanguage.Russian, ["~~~"]);
        var outcome = await Plan().RunAsync([english, russian], Blank);

        Assert.True(outcome.Scan.TooltipFound);
        Assert.Equal(0, russian.Reads);
    }

    [Fact]
    public async Task Without_a_rumour_tooltip_looks_for_an_opened_map()
    {
        var reader = new FakeReader(GameLanguage.English, ["SPRAWLING JUNGLE", "BIOME: OCEAN", "", "", "REQUIRES: WAYSTONE"]);
        var outcome = await Plan().RunAsync([reader], Blank);

        Assert.Equal("sprawling-jungle", outcome.Map?.Island.Id);
        Assert.True(outcome.Clean);
    }

    [Fact]
    public async Task Nothing_on_screen_is_a_miss()
    {
        var outcome = await Plan().RunAsync([new FakeReader(GameLanguage.English, ["WORLD", "INVENTORY"])], Blank);

        Assert.False(outcome.Scan.TooltipFound);
        Assert.Null(outcome.Map);
        Assert.False(outcome.Clean);
    }
}
