using System.Windows.Input;

namespace Rumours.App.Tests;

public class HotkeyTests
{
    private static readonly HotkeyBindings Defaults = new Settings().Hotkeys;

    [Fact]
    public void Defaults_are_f6_f7_f8()
    {
        Assert.Equal("F6", Defaults[HotkeyAction.Scan]);
        Assert.Equal("F7", Defaults[HotkeyAction.Reset]);
        Assert.Equal("F8", Defaults[HotkeyAction.Toggle]);
    }

    [Fact]
    public void A_key_taken_from_another_action_leaves_it_unassigned()
    {
        var (bindings, displaced) = Defaults.Assign(HotkeyAction.Scan, "f7");

        Assert.Equal(HotkeyAction.Reset, displaced);
        Assert.Equal("f7", bindings[HotkeyAction.Scan]);
        Assert.Equal("", bindings[HotkeyAction.Reset]);
        Assert.Equal("F8", bindings[HotkeyAction.Toggle]);
    }

    [Fact]
    public void Clearing_a_key_displaces_nobody()
    {
        var (first, _) = Defaults.Assign(HotkeyAction.Scan, "");
        var (second, displaced) = first.Assign(HotkeyAction.Reset, "");

        Assert.Null(displaced);
        Assert.Equal("", second[HotkeyAction.Scan]);
        Assert.Equal("", second[HotkeyAction.Reset]);
    }

    [Fact]
    public void Bindings_round_trip_through_settings()
    {
        var bindings = Defaults.Assign(HotkeyAction.Toggle, "Ctrl+Shift+D1").Bindings;
        var settings = new Settings().With(bindings);

        Assert.Equal("Ctrl+Shift+D1", settings.ToggleHotkey);
        Assert.Equal("Ctrl+Shift+D1", settings.Hotkeys[HotkeyAction.Toggle]);
    }

    [Theory]
    [InlineData("F6", ModifierKeys.None, Key.F6)]
    [InlineData("ctrl+shift+f6", ModifierKeys.Control | ModifierKeys.Shift, Key.F6)]
    [InlineData(" Alt + OemTilde ", ModifierKeys.Alt, Key.OemTilde)]
    public void Parses_what_the_menu_writes(string hotkey, ModifierKeys modifiers, Key key)
    {
        Assert.True(HotkeyListener.TryParse(hotkey, out var parsedModifiers, out var parsedKey));
        Assert.Equal((modifiers, key), (parsedModifiers, parsedKey));
    }

    [Theory]
    [InlineData("")]
    [InlineData("F66")]
    [InlineData("Hyper+F6")]
    [InlineData("Ctrl+")]
    public void A_misspelt_hotkey_is_refused_not_thrown(string hotkey) =>
        Assert.False(HotkeyListener.TryParse(hotkey, out _, out _));
}
