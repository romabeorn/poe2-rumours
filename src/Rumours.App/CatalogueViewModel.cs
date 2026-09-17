using System.ComponentModel;
using Rumours.Core;

namespace Rumours.App;

public sealed class CatalogueViewModel(RatingSheet sheet, UiText language) : INotifyPropertyChanged
{
    private UiText _text = language;

    private GameLanguage NamesLanguage => _text.Language == UiLanguage.Russian ? GameLanguage.Russian : GameLanguage.English;

    private IslandKind? _filter;

    public void SwitchLanguage(UiText language)
    {
        _text = language;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }

    public IslandKind? Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Table)));
        }
    }

    public IslandTableModel Table => new(_text, IslandCatalog.All
        .Where(i => _filter is null || i.Kind == _filter)
        .OrderBy(i => sheet.For(i).Tier)
        .Select(i => IslandRow.For(i, sheet, _text, NamesLanguage))
        .ToList());

    public event PropertyChangedEventHandler? PropertyChanged;
}
