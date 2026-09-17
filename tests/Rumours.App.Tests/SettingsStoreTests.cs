using System.IO;

namespace Rumours.App.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("poe2rumours-tests-").FullName;

    private string File(string content)
    {
        var path = Path.Combine(_folder, "settings.json");
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void No_file_means_defaults()
    {
        var store = new SettingsStore(Path.Combine(_folder, "settings.json"));

        Assert.Equal(new Settings(), store.Current);
        Assert.Null(store.LoadProblem);
    }

    [Fact]
    public void Reads_a_hand_edited_file_with_comments_and_a_trailing_comma()
    {
        var store = new SettingsStore(File("""
            // edited by hand
            {
              "scanHotkey": "F9", // closer to the fingers
              "overlayLeft": 400,
            }
            """));

        Assert.Equal("F9", store.Current.ScanHotkey);
        Assert.Equal(400, store.Current.OverlayLeft);
        Assert.Equal("F7", store.Current.ResetHotkey);
        Assert.Null(store.LoadProblem);
    }

    [Fact]
    public void A_broken_file_gives_defaults_and_says_why()
    {
        var store = new SettingsStore(File("{ \"scanHotkey\": "));

        Assert.Equal(new Settings(), store.Current);
        Assert.NotNull(store.LoadProblem);
    }

    [Fact]
    public void Out_of_range_values_are_pulled_back()
    {
        var store = new SettingsStore(File("""{ "overlayOpacity": 0, "backdropShade": 7, "scanHotkey": null }"""));

        Assert.Equal(Appearance.MinOverlayOpacity, store.Current.OverlayOpacity);
        Assert.Equal(1, store.Current.BackdropShade);
        Assert.Equal("", store.Current.ScanHotkey);
    }

    [Fact]
    public void Saved_settings_come_back_on_the_next_start()
    {
        var path = Path.Combine(_folder, "settings.json");
        var store = new SettingsStore(path);
        store.Update(s => s with { Language = "en", MenuLeft = -1200.5, ToggleHotkey = "" });
        Assert.Null(store.Flush());

        var reopened = new SettingsStore(path);
        Assert.Equal(store.Current, reopened.Current);
        Assert.False(System.IO.File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Screenshots_are_off_unless_asked_for()
    {
        Assert.Equal(ShotSaving.None, new SettingsStore(Path.Combine(_folder, "settings.json")).Current.SaveShots);
        Assert.Equal(ShotSaving.Failed, new SettingsStore(File("""{ "saveShots": "failed" }""")).Current.SaveShots);
    }

    [Fact]
    public void Announces_changes_but_not_repeats()
    {
        var store = new SettingsStore(Path.Combine(_folder, "settings.json"));
        var announced = 0;
        store.Changed += () => announced++;

        store.Update(s => s with { OverlayLeft = 10 });
        store.Update(s => s with { OverlayLeft = 10 });

        Assert.Equal(1, announced);
    }

    [Fact]
    public void Nothing_is_written_until_something_changes()
    {
        var path = Path.Combine(_folder, "settings.json");
        new SettingsStore(path).Flush();

        Assert.False(System.IO.File.Exists(path));
    }
}
