namespace Rumours.App;

public enum ShotSaving
{
    None,
    Failed,
    All,
}

public sealed record Settings(
    string ScanHotkey = "F6",
    string ResetHotkey = "F7",
    string ToggleHotkey = "F8",
    double OverlayLeft = 90,
    double OverlayTop = 120,
    double? MenuLeft = null,
    double? MenuTop = null,
    ShotSaving SaveShots = ShotSaving.None,
    string Language = "en",
    double OverlayOpacity = 1,
    double BackdropShade = 0.5)
{
    public HotkeyBindings Hotkeys => HotkeyBindings.Of(
        (HotkeyAction.Scan, ScanHotkey), (HotkeyAction.Reset, ResetHotkey), (HotkeyAction.Toggle, ToggleHotkey));

    public Settings With(HotkeyBindings hotkeys) => this with
    {
        ScanHotkey = hotkeys[HotkeyAction.Scan],
        ResetHotkey = hotkeys[HotkeyAction.Reset],
        ToggleHotkey = hotkeys[HotkeyAction.Toggle],
    };

    public Appearance Appearance => new(OverlayOpacity, BackdropShade);

    public Settings With(Appearance appearance) =>
        this with { OverlayOpacity = appearance.OverlayOpacity, BackdropShade = appearance.BackdropShade };

    public Settings Sanitised() => With(Appearance) with
    {
        ScanHotkey = ScanHotkey ?? "",
        ResetHotkey = ResetHotkey ?? "",
        ToggleHotkey = ToggleHotkey ?? "",
        Language = Language ?? "en",
    };
}

public sealed record Appearance
{
    public const double MinOverlayOpacity = 0.3;

    public double OverlayOpacity { get; }

    public double BackdropShade { get; }

    public Appearance(double overlayOpacity, double backdropShade)
    {
        OverlayOpacity = Math.Clamp(overlayOpacity, MinOverlayOpacity, 1);
        BackdropShade = Math.Clamp(backdropShade, 0, 1);
    }
}
