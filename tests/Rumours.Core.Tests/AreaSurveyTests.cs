namespace Rumours.Core.Tests;

public class AreaSurveyTests
{
    private static readonly RatingSheet Sheet = RatingSheet.Parse($$"""
        {
          "policy": { "openAtGrandExpeditions": 4, "openAtTier": "A+" },
          "islands": { {{string.Join(",", IslandCatalog.All.Select(i =>
              $"\"{i.Id}\": {{ \"tier\": \"{(i.Id == "moor" ? "S+" : "C")}\", \"reward\": \"x\" }}"))}} }
        }
        """);

    private static readonly RatingSheet Shipped = RatingSheet.Parse(File.ReadAllText("ratings.json"));

    private static TooltipScan Scan(params string[] ids) => new(ids.Select(IslandCatalog.ById).ToList(), 0);

    private static TooltipScan PartScan(int unrecognised, params string[] ids) =>
        new(ids.Select(IslandCatalog.ById).ToList(), unrecognised);

    [Fact]
    public void Default_sheet_rates_every_island()
    {
        foreach (var island in IslandCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(Shipped.For(island).Reward.In(UiLanguage.Russian)));
            Assert.False(string.IsNullOrWhiteSpace(Shipped.For(island).Reward.In(UiLanguage.English)));
        }
    }

    [Fact]
    public void Rating_text_may_be_a_plain_string_or_a_pair()
    {
        var sheet = RatingSheet.Parse("""
            { "policy": { "openAtGrandExpeditions": 4, "openAtTier": "A+" }, "islands": { ISLANDS,
              "barren-atoll": { "tier": "B+", "reward": { "ru": "Золото", "en": "Gold" } },
              "island-from-a-future-patch": { "tier": "S", "reward": "?" } } }
            """.Replace("ISLANDS", string.Join(",", IslandCatalog.All.Where(i => i.Id != "barren-atoll")
                .Select(i => $"\"{i.Id}\": {{ \"tier\": \"B\", \"reward\": \"Leylines\" }}"))));

        Assert.Equal("Gold", sheet.For(IslandCatalog.ById("barren-atoll")).Reward.In(UiLanguage.English));
        Assert.Equal("Золото", sheet.For(IslandCatalog.ById("barren-atoll")).Reward.In(UiLanguage.Russian));
        Assert.Equal("Leylines", sheet.For(IslandCatalog.ById("exhumed-ruins")).Reward.In(UiLanguage.Russian));
        Assert.Null(sheet.For(IslandCatalog.ById("moor")).Note);
    }

    [Fact]
    public void A_sheet_missing_an_island_is_rejected() =>
        Assert.Throws<FormatException>(() => RatingSheet.Parse(
            """{ "policy": { "openAtGrandExpeditions": 4, "openAtTier": "A+" }, "islands": { "moor": { "tier": "S+", "reward": "x" } } }"""));

    [Fact]
    public void Accumulates_union_and_reports_only_new_islands()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(Scan("bleached-shoals", "craggy-peninsula", "moment-of-zen"));
        var fresh = survey.AddScan(Scan("craggy-peninsula", "lush-isle", "moment-of-zen"));

        Assert.Equal(["lush-isle"], fresh.Select(i => i.Id));
        Assert.Equal(4, survey.Islands.Count);
        Assert.Equal(3, survey.GrandExpeditions);
    }

    [Fact]
    public void Fewer_than_three_rumours_means_the_full_set_is_known()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(Scan("sloughed-gully", "untainted-paradise"));

        Assert.True(survey.FullSetSeen);
        Assert.Equal(Verdict.Skip, survey.Verdict);
    }

    [Fact]
    public void Unreadable_line_does_not_count_as_a_short_list()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(PartScan(1, "sloughed-gully", "untainted-paradise"));

        Assert.False(survey.FullSetSeen);
        Assert.Equal(Verdict.KeepScanning, survey.Verdict);
    }

    [Fact]
    public void Counts_scans_without_news_but_leaves_the_stopping_point_to_the_player()
    {
        var survey = new AreaSurvey(Sheet);
        var shown = Scan("sloughed-gully", "untainted-paradise", "lush-isle");
        survey.AddScan(shown);

        for (var i = 0; i < 20; i++) survey.AddScan(shown);

        Assert.Equal(20, survey.StaleScans);
        Assert.Equal(Verdict.KeepScanning, survey.Verdict);
    }

    [Fact]
    public void New_island_resets_the_count_of_scans_without_news()
    {
        var survey = new AreaSurvey(Sheet);
        var shown = Scan("sloughed-gully", "untainted-paradise", "lush-isle");
        for (var i = 0; i < 3; i++) survey.AddScan(shown);

        survey.AddScan(Scan("sloughed-gully", "untainted-paradise", "exhumed-ruins"));

        Assert.Equal(0, survey.StaleScans);
    }

    [Fact]
    public void Partly_read_scans_do_not_count_as_scans_without_news()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(Scan("sloughed-gully", "untainted-paradise", "lush-isle"));

        for (var i = 0; i < 5; i++) survey.AddScan(PartScan(2, "sloughed-gully"));

        Assert.Equal(0, survey.StaleScans);
    }

    [Fact]
    public void A_reading_without_known_layout_never_proves_the_list_is_short()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(new TooltipScan([IslandCatalog.ById("sloughed-gully")], 0, LayoutKnown: false));

        Assert.False(survey.FullSetSeen);
        Assert.Equal(Verdict.KeepScanning, survey.Verdict);
    }

    [Fact]
    public void Missed_scan_is_ignored()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(TooltipScan.Miss);

        Assert.Equal(0, survey.Scans);
        Assert.False(survey.FullSetSeen);
    }

    [Fact]
    public void Top_tier_island_opens_the_area_at_once()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(Scan("moor", "sloughed-gully", "untainted-paradise"));

        Assert.Equal(Verdict.Open, survey.Verdict);
    }

    [Fact]
    public void Four_grand_expeditions_open_the_area()
    {
        var survey = new AreaSurvey(Sheet);
        survey.AddScan(Scan("lush-isle", "exhumed-ruins", "barren-atoll"));
        Assert.Equal(Verdict.KeepScanning, survey.Verdict);

        survey.AddScan(Scan("lush-isle", "exhumed-ruins", "sloughed-gully"));
        Assert.Equal(Verdict.Open, survey.Verdict);
    }
}
