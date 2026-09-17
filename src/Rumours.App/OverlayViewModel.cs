using System.ComponentModel;
using System.Windows.Media;
using Rumours.Core;

namespace Rumours.App;

public sealed class OverlayViewModel : INotifyPropertyChanged
{
    private static Brush Gold => Palette.Theme("GoldBrush");
    private static Brush Green => Palette.Theme("VerdictOpenBrush");
    private static Brush Red => Palette.Theme("VerdictSkipBrush");
    private static Brush Orange => Palette.Theme("WarningBrush");
    private static Brush Pale => Palette.Theme("LabelBrush");

    private readonly RatingSheet _sheet;
    private Settings _settings;
    private UiText _text;

    public OverlayViewModel(RatingSheet sheet, Settings settings, UiText text)
    {
        (_sheet, _settings, _text) = (sheet, settings, text);
        _survey = new AreaSurvey(sheet);
        (Survey, Inspected) = (Table([]), Table([]));
    }

    private AreaSurvey _survey;
    private IReadOnlyList<Island> _fresh = [];
    private Island? _inspected;
    private string _status = "";
    private bool _warning;

    public UiText Text => _text;

    public GameLanguage GameLanguage { get; private set; }

    public string Status => _status;
    public Brush StatusBrush => _warning ? Orange : Pale;
    public string Hint => _text.Hint(_settings.Hotkeys);

    private bool ShowingMap => _inspected is not null;

    public string VerdictText => ShowingMap ? _text.MapUnderCursor : _survey.Verdict switch
    {
        Verdict.Open => _text.VerdictOpen,
        Verdict.Skip => _text.VerdictSkip,
        _ when _survey.Scans == 0 => _text.VerdictHover,
        _ => _text.VerdictKeepScanning,
    };

    public string Advice => ShowingMap ? "" : _survey.Verdict switch
    {
        Verdict.KeepScanning when _survey.Scans == 0 => _text.AdviceHover(_text.KeyName(_settings.ScanHotkey)),
        Verdict.KeepScanning => _text.AdviceKeepScanning(_text.KeyName(_settings.ScanHotkey)),
        Verdict.Open => _text.AdviceOpen,
        _ => _text.AdviceSkip,
    };

    public Brush VerdictBrush => ShowingMap ? Gold : _survey.Verdict switch
    {
        Verdict.Open => Green,
        Verdict.Skip => Red,
        _ => Gold,
    };

    public bool HasScans => !ShowingMap && _survey.Scans > 0;
    public int Scans => _survey.Scans;
    public int StaleScans => _survey.StaleScans;
    public int IslandCount => _survey.Islands.Count;
    public int GrandExpeditions => _survey.GrandExpeditions;
    public string Completeness => !ShowingMap && _survey.FullSetSeen ? _text.FullSetSeen : "";

    public IslandTableModel Survey { get; private set; }

    public IslandTableModel Inspected { get; private set; }

    private IslandTableModel Table(IEnumerable<Island> islands) => new(_text, islands
        .OrderBy(i => _sheet.For(i).Tier)
        .Select(i => IslandRow.For(i, _sheet, _text, GameLanguage, _fresh.Contains(i)))
        .ToList());

    public void Apply(TooltipScan scan)
    {
        if (scan.Recognised.Count == 0)
        {
            Report(scan.TooltipFound ? _text.TooltipUnread : _text.TooltipMissing, warning: true);
            return;
        }

        GameLanguage = scan.Language;
        _inspected = null;
        _fresh = _survey.AddScan(scan);
        if (!scan.Complete)
            Report(_text.PartlyRead(scan.Recognised.Count, Math.Max(scan.Rows, scan.Recognised.Count + 1)), warning: true);
        else
            Report(_fresh.Count == 0 ? _text.NothingNew : _text.NewIslands(_fresh.Count), warning: false);
    }

    public void Inspect(Island map, GameLanguage language)
    {
        GameLanguage = language;
        _survey = new AreaSurvey(_sheet);
        (_fresh, _inspected) = ([], map);
        Report("", warning: false);
    }

    public void Fail(string reason) => Report(_text.ScanFailed(reason), warning: true);

    public void Reset()
    {
        _survey = new AreaSurvey(_sheet);
        (_fresh, _inspected) = ([], null);
        Report("", warning: false);
    }

    public void UpdateSettings(Settings updated)
    {
        _settings = updated;
        Report(_status, _warning);
    }

    public void SwitchLanguage(UiText language)
    {
        _text = language;
        Report("", warning: false);
    }

    private void Report(string status, bool warning)
    {
        (_status, _warning) = (status, warning);
        Survey = Table(ShowingMap ? [] : _survey.Islands);
        Inspected = Table(_inspected is null ? [] : [_inspected]);
        RaiseAll();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RaiseAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
}
