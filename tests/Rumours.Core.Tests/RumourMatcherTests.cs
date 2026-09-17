namespace Rumours.Core.Tests;

public class RumourMatcherTests
{
    private readonly RumourMatcher _matcher = new(IslandCatalog.All);

    [Fact]
    public void Every_rumour_matches_itself_in_both_languages()
    {
        foreach (var island in IslandCatalog.All)
            foreach (var (_, text) in island.Texts)
                Assert.Equal(island, _matcher.Match(text.Rumour));
    }

    [Theory]
    [InlineData("Сла8ный малый...", "moment-of-zen")]
    [InlineData("Бескрайние скалы.-", "craggy-peninsula")]
    [InlineData("Мрачна и ужасна...", "bleached-shoals")]
    public void Reads_the_russian_client(string line, string expectedId) =>
        Assert.Equal(expectedId, _matcher.Match(line)?.Id);

    [Theory]
    [InlineData("Endless clif fs...", "craggy-peninsula")]
    [InlineData("Bleak and awful…", "bleached-shoals")]
    [InlineData("A qood fellow..", "moment-of-zen")]
    [InlineData("Nothin to drlnk", "stagnant-basin")]
    [InlineData("SULPHITE|", "scorched-cay")]
    [InlineData("lt's dry at Ieast", "sloughed-gully")]
    [InlineData("Orlgin of the fa11", "obscure-island")]
    public void Tolerates_ocr_noise(string line, string expectedId) =>
        Assert.Equal(expectedId, _matcher.Match(line)?.Id);

    [Theory]
    [InlineData("Uwkywww miws...", "exhumed-ruins")]
    [InlineData("uwhwww miws...", "exhumed-ruins")]
    [InlineData("luwhwww miws...", "exhumed-ruins")]
    [InlineData("Uwhwww rflws...", "exhumed-ruins")]
    [InlineData("uwhwww Nib'...", "exhumed-ruins")]
    [InlineData("A 4004 fellow...", "moment-of-zen")]
    [InlineData("A fellow...", "moment-of-zen")]
    [InlineData("Ebd circle...", "sprawling-jungle")]
    [InlineData("Ewd of doe circle...", "sprawling-jungle")]
    [InlineData("Lod•viw' to driwke..", "stagnant-basin")]
    [InlineData("nnw' to driwke..", "stagnant-basin")]
    [InlineData("woÖviw' to dribk..", "stagnant-basin")]
    [InlineData("Wild romiwq free...", "lush-isle")]
    [InlineData("BleakaU awful...", "bleached-shoals")]
    [InlineData("Its at lust..", "sloughed-gully")]
    [InlineData("Waru hit riso...", "grazed-prairie")]
    public void Reads_what_windows_ocr_makes_of_the_handwritten_font(string line, string expectedId) =>
        Assert.Equal(expectedId, _matcher.Match(line)?.Id);

    [Fact]
    public void Refuses_to_guess_between_two_equally_close_rumours() =>
        Assert.Null(_matcher.Match("to driwk.."));

    [Theory]
    [InlineData("Island Rumours")]
    [InlineData("Use a Logbook to chart the area")]
    [InlineData("Requires:")]
    [InlineData("Expedition Logbook")]
    [InlineData("Uncharted Waters")]
    [InlineData("Слухи об острове")]
    [InlineData("Журнал экспедиции")]
    [InlineData("Требует:")]
    [InlineData("")]
    public void Ignores_other_tooltip_lines(string line) =>
        Assert.Null(_matcher.Match(line));
}
