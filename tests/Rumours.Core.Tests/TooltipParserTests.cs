namespace Rumours.Core.Tests;

public class TooltipParserTests
{
    private readonly TooltipParser _parser = new(new RumourMatcher(IslandCatalog.All));

    private const int HeaderY = 393, FirstRowY = 437, RowPitch = 43, RequiresY = 572;

    private static TextLine Line(string text, double y, double centerX = 725, double width = 150) =>
        new(text, centerX - width / 2, y, width, 24);

    private static List<TextLine> Tooltip(params string[] rumours)
    {
        var lines = new List<TextLine>
        {
            Line("Search here", 29, centerX: 80),
            Line("Uncharted Waters", 308, width: 289),
            Line("Use a Logbook to chart the area", 358, width: 325),
            Line("Island Rumours", HeaderY, width: 182),
        };
        lines.AddRange(rumours.Select((r, i) => Line(r, FirstRowY + i * RowPitch)));
        lines.Add(Line("Requires:", RequiresY, width: 111));
        lines.Add(Line("Expedition Logbook", RequiresY + 34, width: 209));
        return lines;
    }

    private static GreyImage Parchment(params int[] writtenSlots)
    {
        const int width = 1440, height = 700;
        var luma = Enumerable.Repeat((byte)185, width * height).ToArray();
        foreach (var slot in writtenSlots)
            for (var y = FirstRowY + slot * RowPitch + 4; y < FirstRowY + slot * RowPitch + 20; y++)
                for (var x = 670; x < 780; x += 3) luma[y * width + x] = 30;
        return new GreyImage(width, height, luma);
    }

    [Fact]
    public void Reads_three_rumours_under_the_header()
    {
        var scan = _parser.Parse(Tooltip("Bleak and awful...", "Endless clif fs...", "A good fellow..."));

        Assert.Equal(["bleached-shoals", "craggy-peninsula", "moment-of-zen"], scan.Recognised.Select(i => i.Id));
        Assert.Equal(0, scan.UnrecognisedLines);
    }

    [Fact]
    public void Counts_a_line_it_could_not_read()
    {
        var scan = _parser.Parse(Tooltip("Bleak and awful...", "~~~ ~~ ~~~~", "A good fellow..."));

        Assert.Equal(2, scan.Recognised.Count);
        Assert.Equal(1, scan.UnrecognisedLines);
    }

    [Fact]
    public void Short_list_stops_at_requires()
    {
        var scan = _parser.Parse(Tooltip("Sulphite!"));

        Assert.Equal(["scorched-cay"], scan.Recognised.Select(i => i.Id));
        Assert.Equal(0, scan.UnrecognisedLines);
    }

    [Fact]
    public void No_anchor_means_no_tooltip()
    {
        var lines = Tooltip("Sulphite!").Where(l => l.Text is not ("Island Rumours" or "Use a Logbook to chart the area")).ToList();

        Assert.False(_parser.Parse(lines).TooltipFound);
    }

    [Fact]
    public void Ignores_text_outside_the_tooltip_column()
    {
        var lines = Tooltip("Sulphite!", "Cold as ice...");
        lines.Add(Line("Fallen stars...", 480, centerX: 200));

        Assert.Equal(["scorched-cay", "frigid-bluffs"], _parser.Parse(lines).Recognised.Select(i => i.Id));
    }

    [Fact]
    public void Falls_back_to_the_intro_line_when_the_header_is_garbled()
    {
        var lines = Tooltip("Sulphite!", "Cold as ice...")
            .Select(l => l.Text == "Island Rumours" ? l with { Text = "ISIAHD RUMDUHS |" } : l).ToList();
        var scan = _parser.Parse(lines);

        Assert.Equal(["scorched-cay", "frigid-bluffs"], scan.Recognised.Select(i => i.Id));
        Assert.Equal(0, scan.UnrecognisedLines);
    }

    [Fact]
    public void Falls_back_to_the_intro_line_when_the_header_is_missing()
    {
        var lines = Tooltip("Sulphite!").Where(l => l.Text != "Island Rumours").ToList();

        Assert.Equal(["scorched-cay"], _parser.Parse(lines).Recognised.Select(i => i.Id));
    }

    [Fact]
    public void Glues_a_rumour_split_into_two_fragments()
    {
        var lines = Tooltip("Bleak and awful...");
        lines.Add(new TextLine("Wild roaming", 640, FirstRowY + RowPitch, 110, 24));
        lines.Add(new TextLine("free...", 765, FirstRowY + RowPitch + 2, 50, 22));
        var scan = _parser.Parse(lines);

        Assert.Equal(["bleached-shoals", "lush-isle"], scan.Recognised.Select(i => i.Id));
        Assert.Equal(0, scan.UnrecognisedLines);
    }

    [Fact]
    public void Ink_in_a_slot_betrays_a_row_the_ocr_never_saw()
    {
        var lines = Tooltip("End of the circle...", "Cold as ice...", "Warm but risky...");
        lines.RemoveAll(l => l.Text == "Cold as ice...");
        var scan = _parser.Parse(lines, Parchment(0, 1, 2));

        Assert.Equal(["sprawling-jungle", "grazed-prairie"], scan.Recognised.Select(i => i.Id));
        Assert.Equal(1, scan.UnrecognisedLines);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Blank_slots_mean_the_list_really_is_short(int rows)
    {
        string[] rumours = ["Sulphite!", "Cold as ice...", "Fallen stars..."];
        var scan = _parser.Parse(Tooltip(rumours[..rows]), Parchment(Enumerable.Range(0, rows).ToArray()));

        Assert.Equal(rows, scan.Recognised.Count);
        Assert.Equal(0, scan.UnrecognisedLines);
    }

    [Fact]
    public void Dark_background_behind_a_slot_is_not_ink()
    {
        var night = new GreyImage(1440, 700, new byte[1440 * 700]);

        Assert.Equal(0, _parser.Parse(Tooltip("Sulphite!"), night).UnrecognisedLines);
    }

    [Fact]
    public void Reads_the_russian_client_and_says_so()
    {
        var translation = new Dictionary<string, string>
        {
            ["Uncharted Waters"] = "Неизведанные воды",
            ["Use a Logbook to chart the area"] = "Используйте журнал, чтобы нанести область на карту",
            ["Island Rumours"] = "слухи об острове",
            ["Requires:"] = "Требует:",
            ["Expedition Logbook"] = "Журнал экспедиции",
        };
        var lines = Tooltip("Сла8ный малый...", "Бескрайние скалы.-", "Мрачна и ужасна...")
            .Select(l => translation.TryGetValue(l.Text, out var russian) ? l with { Text = russian } : l).ToList();
        var scan = _parser.Parse(lines, Parchment(0, 1, 2));

        Assert.Equal(GameLanguage.Russian, scan.Language);
        Assert.Equal(["moment-of-zen", "craggy-peninsula", "bleached-shoals"], scan.Recognised.Select(i => i.Id));
        Assert.Equal(0, scan.UnrecognisedLines);
    }

    [Fact]
    public void Lost_header_and_unreadable_first_rumour_is_not_a_short_list()
    {
        var lines = Tooltip("~~~ ~~ ~~~~", "Cold as ice...").Where(l => l.Text != "Island Rumours").ToList();
        var scan = _parser.Parse(lines, Parchment(0, 1));

        Assert.Equal(["frigid-bluffs"], scan.Recognised.Select(i => i.Id));
        Assert.Equal(1, scan.UnrecognisedLines);
        Assert.False(scan.Complete);
    }

    [Fact]
    public void Garbled_header_does_not_land_in_a_slot()
    {
        var lines = Tooltip("Sulphite!")
            .Select(l => l.Text == "Island Rumours" ? l with { Text = "ISIAHD RUMDUHS |" } : l).ToList();
        var scan = _parser.Parse(lines, Parchment(0));

        Assert.Equal(["scorched-cay"], scan.Recognised.Select(i => i.Id));
        Assert.True(scan.Complete);
    }

    [Fact]
    public void Without_the_footer_the_reading_is_never_complete()
    {
        var lines = Tooltip("Sulphite!").Where(l => l.Text != "Requires:").ToList();
        var scan = _parser.Parse(lines, Parchment(0));

        Assert.Equal(["scorched-cay"], scan.Recognised.Select(i => i.Id));
        Assert.False(scan.LayoutKnown);
        Assert.False(scan.Complete);
    }

    [Fact]
    public void A_rumour_outside_the_slots_means_the_layout_has_changed()
    {
        var lines = Tooltip("Sulphite!");
        lines.Add(Line("Cold as ice...", FirstRowY + RowPitch + RowPitch / 2));
        var scan = _parser.Parse(lines, Parchment(0));

        Assert.False(scan.Complete);
    }

    [Fact]
    public void A_complete_reading_beats_a_union_with_a_blind_one()
    {
        var complete = _parser.Parse(Tooltip("Sulphite!"), Parchment(0));
        var blind = _parser.Parse(Tooltip("Sulphite!").Where(l => l.Text != "Requires:").ToList(), Parchment(0));

        Assert.True(TooltipScan.Combine([blind, complete]).Complete);
    }

    [Fact]
    public void Four_different_rumours_cannot_come_from_one_tooltip()
    {
        var first = _parser.Parse(Tooltip("Sulphite!", "~~~ ~~~", "Cold as ice..."));
        var second = _parser.Parse(Tooltip("Sulphite!", "Fallen stars...", "~~~ ~~~"));
        var third = _parser.Parse(Tooltip("Sulphite!", "~~~ ~~~", "Almost paradise."));
        var combined = TooltipScan.Combine([first, second, third]);

        Assert.Equal(["scorched-cay"], combined.Recognised.Select(i => i.Id));
        Assert.False(combined.Complete);
    }

    [Fact]
    public void Combined_readings_cover_each_others_gaps()
    {
        var first = _parser.Parse(Tooltip("Bleak and awful...", "~~~ ~~ ~~~~", "A good fellow..."));
        var second = _parser.Parse(Tooltip("~~~~ ~~~", "Endless cliffs...", "A good fellow..."));
        var combined = TooltipScan.Combine([first, second, TooltipScan.Miss]);

        Assert.Equal(3, combined.Recognised.Count);
        Assert.Equal(0, combined.UnrecognisedLines);
    }

    [Fact]
    public void Combining_only_misses_is_a_miss()
    {
        Assert.False(TooltipScan.Combine([TooltipScan.Miss, TooltipScan.Miss]).TooltipFound);
    }
}
