using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Rumours.Capture;
using Rumours.Core;

namespace Rumours.App;

public partial class App : Application
{
    private const string Name = "PoE2 Rumours";

    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(400);

    private SettingsStore _store = null!;
    private RatingSheet _sheet = null!;
    private UiText _text = null!;
    private ScanLog _log = null!;
    private OverlayViewModel _viewModel = null!;
    private OverlayWindow _window = null!;
    private ScanController _scans = null!;
    private HotkeyListener _hotkeys = null!;
    private TrayMenu _tray = null!;
    private DispatcherTimer _saveTimer = null!;
    private MenuWindow? _menuWindow;
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var paths = new AppPaths();
            _store = new SettingsStore(paths.Settings);
            _text = UiText.For(_store.Current.Language);
            _sheet = LoadRatings(paths, out var ratingsProblem);
            _log = new ScanLog(paths.Logs, _store.Current.SaveShots);

            ThemeTextures.Apply(Resources, paths.Theme);
            Resources["BackdropShade"] = _store.Current.BackdropShade;

            if (e.Args.Length > 0)
            {
                if (!OverlayPreview.TryRun(e.Args, _sheet, _store.Current, _text))
                    MessageBox.Show(_text.UnknownArguments(string.Join(' ', e.Args)), Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            _singleInstance = new Mutex(initiallyOwned: true, "Poe2Rumours.SingleInstance", out var first);
            if (!first)
            {
                MessageBox.Show(_text.AlreadyRunning, Name, MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            DispatcherUnhandledException += (_, failure) =>
            {
                _log.WriteError(failure.Exception);
                _viewModel.Fail(failure.Exception.Message);
                failure.Handled = true;
            };

            _viewModel = new OverlayViewModel(_sheet, _store.Current, _text);
            _window = new OverlayWindow(_viewModel, _store.Current);
            _window.Moved += position =>
                _store.Update(s => s with { OverlayLeft = position.X, OverlayTop = position.Y });
            Apply(_store.Current.Appearance);
            _window.Show();

            _saveTimer = new DispatcherTimer { Interval = SaveDelay };
            _saveTimer.Tick += (_, _) => Save();
            _store.Changed += () =>
            {
                _saveTimer.Stop();
                _saveTimer.Start();
            };

            _scans = new ScanController(TooltipScanner.ForGame(), _log, _viewModel, _window);
            _hotkeys = new HotkeyListener(_window.Source);
            RegisterHotkeys();

            _tray = new TrayMenu(_text, OpenMenu, Shutdown);
            _tray.Announce(_text.StartedTitle, _text.StartedBody(_text.KeyName(_store.Current.ScanHotkey)));

            if (_store.LoadProblem is { } problem)
                MessageBox.Show(_text.SettingsUnreadable(problem), Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            if (ratingsProblem is not null)
                MessageBox.Show(_text.RatingsUnreadable(ratingsProblem), Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, UiText.For("en").StartupFailed, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static RatingSheet LoadRatings(AppPaths paths, out string? problem)
    {
        problem = null;
        try
        {
            if (paths.CustomRatings() is { } custom) return RatingSheet.Parse(custom);
        }
        catch (Exception error) when (error is FormatException or System.Text.Json.JsonException or KeyNotFoundException or IOException)
        {
            problem = error.Message;
        }
        return RatingSheet.Parse(AppPaths.ShippedRatings());
    }

    private void Save()
    {
        _saveTimer.Stop();
        if (_store.Flush() is { } problem) _viewModel.Fail(_text.SettingsNotSaved(problem));
    }

    private void ToggleOverlay()
    {
        if (_window.IsVisible) _window.Hide();
        else _window.Show();
    }

    private void RegisterHotkeys()
    {
        _hotkeys.Clear();
        var hotkeys = _store.Current.Hotkeys;
        var refused = new (HotkeyAction Action, Action Run)[]
        {
            (HotkeyAction.Scan, _scans.Scan),
            (HotkeyAction.Reset, _viewModel.Reset),
            (HotkeyAction.Toggle, ToggleOverlay),
        }.Where(h => !_hotkeys.Register(hotkeys[h.Action], h.Run)).Select(h => hotkeys[h.Action]).ToList();

        if (refused.Count > 0)
            MessageBox.Show(_text.HotkeysRefused(string.Join(", ", refused)), Name, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Apply(Appearance appearance)
    {
        _window.Opacity = appearance.OverlayOpacity;
        Resources["BackdropShade"] = appearance.BackdropShade;
    }

    private void OpenLogs()
    {
        Directory.CreateDirectory(_log.Folder);
        Process.Start(new ProcessStartInfo(_log.Folder) { UseShellExecute = true });
    }

    private void OpenMenu()
    {
        if (_menuWindow is not null)
        {
            _menuWindow.Activate();
            return;
        }

        var settings = _store.Current;
        var menu = new MenuWindow(new MenuViewModel(_text, new CatalogueViewModel(_sheet, _text), settings.Hotkeys, settings.Appearance));
        if (settings is { MenuLeft: { } left, MenuTop: { } top })
        {
            menu.WindowStartupLocation = WindowStartupLocation.Manual;
            (menu.Left, menu.Top) = ScreenPlacement.Visible(new Point(left, top));
        }

        var model = menu.ViewModel;
        model.LogsRequested += OpenLogs;
        model.LanguagePicked += SwitchLanguage;
        model.MoveOverlayToggled += moving => _window.SetMovable(moving, _text.MoveOverlayHint);
        model.AppearanceChanged += appearance =>
        {
            _store.Update(s => s.With(appearance));
            Apply(appearance);
        };
        model.HotkeysChanged += hotkeys =>
        {
            _store.Update(s => s.With(hotkeys));
            _viewModel.UpdateSettings(_store.Current);
        };
        model.ListeningChanged += listening =>
        {
            if (listening) _hotkeys.Clear();
            else RegisterHotkeys();
        };
        menu.Closed += (_, _) =>
        {
            model.Close();
            _store.Update(s => s with { MenuLeft = menu.Left, MenuTop = menu.Top });
            _menuWindow = null;
        };

        _menuWindow = menu;
        menu.Show();
        menu.Activate();
    }

    private void SwitchLanguage(UiLanguage language)
    {
        _text = new UiText(language);
        _menuWindow?.ViewModel.SwitchLanguage(_text);
        _viewModel.SwitchLanguage(_text);
        _tray.SwitchLanguage(_text);
        _store.Update(s => s with { Language = _text.SettingValue });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _store?.Flush();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
