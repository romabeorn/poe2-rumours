using System.ComponentModel;
using System.Windows.Input;
using Rumours.Core;

namespace Rumours.App;

public sealed record HotkeyRow(HotkeyAction Action, string Name, string Key, bool Listening, ICommand Rebind);

public sealed class MenuViewModel : INotifyPropertyChanged
{
    private UiText _text;
    private HotkeyBindings _hotkeys;
    private HotkeyAction? _listeningFor;
    private bool _onSettings, _moving;
    private double _opacity, _shade;
    private string _warning = "";

    public event Action? LogsRequested;
    public event Action<UiLanguage>? LanguagePicked;
    public event Action<Appearance>? AppearanceChanged;
    public event Action<HotkeyBindings>? HotkeysChanged;

    public event Action<bool>? MoveOverlayToggled;

    public event Action<bool>? ListeningChanged;

    public MenuViewModel(UiText text, CatalogueViewModel catalogue, HotkeyBindings hotkeys, Appearance appearance)
    {
        (_text, Catalogue, _hotkeys) = (text, catalogue, hotkeys);
        (_opacity, _shade) = (appearance.OverlayOpacity, appearance.BackdropShade);
        OpenLogs = new Command(() => LogsRequested?.Invoke());
    }

    public UiText Text => _text;
    public string Version => AppInfo.Title;
    public CatalogueViewModel Catalogue { get; }
    public ICommand OpenLogs { get; }

    public bool OnIslands { get => !_onSettings; set => ShowSettings(!value); }
    public bool OnSettings { get => _onSettings; set => ShowSettings(value); }

    public void ShowSettings(bool settings = true)
    {
        _onSettings = settings;
        RaiseAll();
    }

    public bool ShowAll { get => Catalogue.Filter is null; set => Pick(value, null); }
    public bool ShowExpeditions { get => Catalogue.Filter == IslandKind.GrandExpedition; set => Pick(value, IslandKind.GrandExpedition); }
    public bool ShowBosses { get => Catalogue.Filter == IslandKind.Boss; set => Pick(value, IslandKind.Boss); }
    public bool ShowUniques { get => Catalogue.Filter == IslandKind.UniqueMap; set => Pick(value, IslandKind.UniqueMap); }

    private void Pick(bool chosen, IslandKind? filter)
    {
        if (!chosen || Catalogue.Filter == filter) return;
        Catalogue.Filter = filter;
        RaiseAll();
    }

    public bool Russian { get => _text.Language == UiLanguage.Russian; set => Pick(value, UiLanguage.Russian); }
    public bool English { get => _text.Language == UiLanguage.English; set => Pick(value, UiLanguage.English); }

    private void Pick(bool chosen, UiLanguage language)
    {
        if (chosen && _text.Language != language) LanguagePicked?.Invoke(language);
    }

    public void SwitchLanguage(UiText text)
    {
        _text = text;
        _warning = "";
        Catalogue.SwitchLanguage(text);
        RaiseAll();
    }

    public double MinOpacityPercent => Appearance.MinOverlayOpacity * 100;
    public double OpacityPercent { get => _opacity * 100; set => Look(value / 100, _shade); }
    public double ShadePercent { get => _shade * 100; set => Look(_opacity, value / 100); }
    public string OpacityLabel => $"{OpacityPercent:0}%";
    public string ShadeLabel => $"{ShadePercent:0}%";

    private void Look(double opacity, double shade)
    {
        var appearance = new Appearance(opacity, shade);
        if (appearance.OverlayOpacity == _opacity && appearance.BackdropShade == _shade) return;

        (_opacity, _shade) = (appearance.OverlayOpacity, appearance.BackdropShade);
        RaiseAll();
        AppearanceChanged?.Invoke(appearance);
    }

    public bool Moving
    {
        get => _moving;
        set
        {
            if (_moving == value) return;
            _moving = value;
            RaiseAll();
            MoveOverlayToggled?.Invoke(value);
        }
    }

    public string MoveLabel => _moving ? _text.MoveOverlayDone : _text.MoveOverlay;

    public string Warning => _warning;
    public bool IsListening => _listeningFor is not null;

    public IReadOnlyList<HotkeyRow> Hotkeys => Enum.GetValues<HotkeyAction>().Select(action =>
    {
        var listening = action == _listeningFor;
        var key = listening ? _text.PressAKey : _text.KeyOrNotAssigned(_hotkeys[action]);
        return new HotkeyRow(action, _text.ActionName(action), key, listening, new Command(() => Listen(action)));
    }).ToList();

    private void Listen(HotkeyAction action)
    {
        if (_listeningFor is null) ListeningChanged?.Invoke(true);
        (_listeningFor, _warning) = (action, "");
        RaiseAll();
    }

    public void KeyPressed(string hotkey)
    {
        if (_listeningFor is not { } action) return;

        var (hotkeys, displaced) = _hotkeys.Assign(action, hotkey);
        _hotkeys = hotkeys;
        _warning = displaced is { } other ? _text.HotkeyMoved(hotkey, _text.ActionName(other)) : "";

        HotkeysChanged?.Invoke(hotkeys);
        StopListening();
        RaiseAll();
    }

    private void StopListening()
    {
        if (_listeningFor is null) return;
        _listeningFor = null;
        ListeningChanged?.Invoke(false);
    }

    public void Close()
    {
        StopListening();
        Moving = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RaiseAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));

    private sealed class Command(Action run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => run();
    }
}
