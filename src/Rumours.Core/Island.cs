namespace Rumours.Core;

public enum IslandKind
{
    GrandExpedition,
    UniqueMap,
    Boss,
}

public sealed record IslandText(string Rumour, string Map);

public sealed class Island
{
    private readonly IReadOnlyDictionary<GameLanguage, IslandText> _texts;

    public string Id { get; }
    public IslandKind Kind { get; }

    public IEnumerable<KeyValuePair<GameLanguage, IslandText>> Texts => _texts;

    public Island(string id, IslandKind kind, params (GameLanguage Language, string Rumour, string Map)[] texts)
    {
        (Id, Kind) = (id, kind);
        _texts = texts.ToDictionary(t => t.Language, t => new IslandText(t.Rumour, t.Map));
        if (!_texts.ContainsKey(GameLanguage.English))
            throw new ArgumentException($"Island {id} has no English texts; they are the fallback for every other language");
    }

    public IslandText In(GameLanguage language) =>
        _texts.TryGetValue(language, out var text) ? text : _texts[GameLanguage.English];

    public override string ToString() => Id;
}
