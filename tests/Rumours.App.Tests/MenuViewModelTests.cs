using Rumours.Core;

namespace Rumours.App.Tests;

public class MenuViewModelTests
{
    private const string Ratings = """
        { "policy": { "openAtGrandExpeditions": 4, "openAtTier": "A+" }, "islands": { ISLANDS } }
        """;

    private static RatingSheet Sheet() => RatingSheet.Parse(Ratings.Replace("ISLANDS",
        string.Join(",", IslandCatalog.All.Select(i => $"\"{i.Id}\": {{ \"tier\": \"B\", \"reward\": \"x\" }}"))));

    private static MenuViewModel Menu(UiLanguage language = UiLanguage.Russian)
    {
        var text = new UiText(language);
        return new MenuViewModel(text, new CatalogueViewModel(Sheet(), text), new Settings().Hotkeys, new Appearance(1, 0.5));
    }

    [Fact]
    public void Opens_on_the_islands_tab_with_every_island()
    {
        var menu = Menu();

        Assert.True(menu.OnIslands);
        Assert.True(menu.ShowAll);
        Assert.Equal(IslandCatalog.All.Count, menu.Catalogue.Table.Rows.Count);
    }

    [Fact]
    public void A_nested_tab_filters_the_catalogue()
    {
        var menu = Menu();
        menu.ShowBosses = true;

        Assert.False(menu.ShowAll);
        Assert.Equal(IslandCatalog.All.Count(i => i.Kind == IslandKind.Boss), menu.Catalogue.Table.Rows.Count);
    }

    [Fact]
    public void Rebinding_pauses_global_hotkeys_only_while_waiting_for_the_key()
    {
        var menu = Menu();
        var pauses = new List<bool>();
        HotkeyBindings? changed = null;
        menu.ListeningChanged += pauses.Add;
        menu.HotkeysChanged += bindings => changed = bindings;

        menu.Hotkeys.Single(r => r.Action == HotkeyAction.Scan).Rebind.Execute(null);
        Assert.True(menu.IsListening);
        Assert.True(menu.Hotkeys.Single(r => r.Action == HotkeyAction.Scan).Listening);

        menu.KeyPressed("Ctrl+F9");

        Assert.Equal([true, false], pauses);
        Assert.False(menu.IsListening);
        Assert.Equal("Ctrl+F9", changed?[HotkeyAction.Scan]);
        Assert.Equal("Ctrl+F9", menu.Hotkeys.Single(r => r.Action == HotkeyAction.Scan).Key);
    }

    [Fact]
    public void Escape_clears_the_binding_and_says_so()
    {
        var menu = Menu();
        menu.Hotkeys.Single(r => r.Action == HotkeyAction.Toggle).Rebind.Execute(null);
        menu.KeyPressed("");

        Assert.Equal(menu.Text.NotAssigned, menu.Hotkeys.Single(r => r.Action == HotkeyAction.Toggle).Key);
    }

    [Fact]
    public void Taking_a_key_from_another_action_is_explained()
    {
        var menu = Menu();
        menu.Hotkeys.Single(r => r.Action == HotkeyAction.Scan).Rebind.Execute(null);
        menu.KeyPressed("F7");

        Assert.Contains("F7", menu.Warning);
        Assert.Equal(menu.Text.NotAssigned, menu.Hotkeys.Single(r => r.Action == HotkeyAction.Reset).Key);
    }

    [Fact]
    public void A_key_pressed_when_nobody_asked_is_ignored()
    {
        var menu = Menu();
        var changed = false;
        menu.HotkeysChanged += _ => changed = true;

        menu.KeyPressed("F9");

        Assert.False(changed);
    }

    [Fact]
    public void Sliders_report_appearance_within_limits()
    {
        var menu = Menu();
        Appearance? reported = null;
        menu.AppearanceChanged += appearance => reported = appearance;

        menu.OpacityPercent = 5;
        menu.ShadePercent = 80;

        Assert.Equal(Appearance.MinOverlayOpacity, reported?.OverlayOpacity);
        Assert.Equal(0.8, reported!.BackdropShade, 3);
        Assert.Equal("80%", menu.ShadeLabel);
    }

    [Fact]
    public void Picking_the_current_language_again_is_not_a_change()
    {
        var menu = Menu(UiLanguage.Russian);
        var picked = new List<UiLanguage>();
        menu.LanguagePicked += picked.Add;

        menu.Russian = true;
        menu.English = true;

        Assert.Equal([UiLanguage.English], picked);
    }

    [Fact]
    public void Switching_language_keeps_the_tab_and_the_filter()
    {
        var menu = Menu();
        menu.ShowUniques = true;
        menu.SwitchLanguage(new UiText(UiLanguage.English));

        Assert.True(menu.ShowUniques);
        Assert.Equal("Settings", menu.Text.TabSettings);
        Assert.Equal("Moment of Zen", menu.Catalogue.Table.Rows.Single(r => r.Map.Contains("Zen")).Map);
    }

    [Fact]
    public void Closing_the_window_ends_unfinished_modes()
    {
        var menu = Menu();
        var events = new List<string>();
        menu.ListeningChanged += listening => events.Add($"listening:{listening}");
        menu.MoveOverlayToggled += moving => events.Add($"moving:{moving}");

        menu.Moving = true;
        menu.Hotkeys[0].Rebind.Execute(null);
        menu.Close();

        Assert.Equal(["moving:True", "listening:True", "listening:False", "moving:False"], events);
    }
}
