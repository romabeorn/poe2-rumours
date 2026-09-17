namespace Rumours.Core;

public enum GameLanguage
{
    English,
    Russian,
}

public sealed record GameLocale(GameLanguage Language, string OcrTag, string Header, string Intro, string Footer);

public static class GameLocales
{
    public static IReadOnlyList<GameLocale> All { get; } =
    [
        new(GameLanguage.English, "en-US", "islandrumours", "usealogbooktochartthearea", "requires"),
        new(GameLanguage.Russian, "ru", "слухиобострове", "используйтежурналчтобынанестиобластьнакарту", "требует"),
    ];
}
