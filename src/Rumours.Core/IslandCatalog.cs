namespace Rumours.Core;

public static class IslandCatalog
{
    public static IReadOnlyList<Island> All { get; } =
    [
        new("moor", IslandKind.GrandExpedition,
            (GameLanguage.English, "Fallen stars...", "Moor of Fallen Skies"),
            (GameLanguage.Russian, "Упавшие звёзды...", "Причал Падших небес")),
        new("frigid-bluffs", IslandKind.GrandExpedition,
            (GameLanguage.English, "Cold as ice...", "Frigid Bluffs"),
            (GameLanguage.Russian, "Холодна как лёд...", "Холодные скалы")),
        new("stagnant-basin", IslandKind.GrandExpedition,
            (GameLanguage.English, "Nothin' to drink...", "Stagnant Basin"),
            (GameLanguage.Russian, "Нечего пить...", "Застойный водоём")),
        new("craggy-peninsula", IslandKind.GrandExpedition,
            (GameLanguage.English, "Endless cliffs...", "Craggy Peninsula"),
            (GameLanguage.Russian, "Бескрайние скалы...", "Скалистый мыс")),
        new("scorched-cay", IslandKind.GrandExpedition,
            (GameLanguage.English, "Sulphite!", "Scorched Cay"),
            (GameLanguage.Russian, "Сульфит!", "Выжженный островок")),
        new("exhumed-ruins", IslandKind.GrandExpedition,
            (GameLanguage.English, "Unknown ruins...", "Exhumed Ruins"),
            (GameLanguage.Russian, "Неизвестные руины...", "Разрытые руины")),
        new("barren-atoll", IslandKind.GrandExpedition,
            (GameLanguage.English, "Somethin' fishy...", "Barren Atoll"),
            (GameLanguage.Russian, "Что-то нечисто...", "Бесплодный риф")),
        new("lush-isle", IslandKind.GrandExpedition,
            (GameLanguage.English, "Wild roaming free...", "Lush Isle"),
            (GameLanguage.Russian, "Дикие бродят на воле...", "Зелёный островок")),
        new("bleached-shoals", IslandKind.GrandExpedition,
            (GameLanguage.English, "Bleak and awful...", "Bleached Shoals"),
            (GameLanguage.Russian, "Мрачна и ужасна...", "Выбеленная отмель")),
        new("grazed-prairie", IslandKind.GrandExpedition,
            (GameLanguage.English, "Warm but risky...", "Grazed Prairie"),
            (GameLanguage.Russian, "Тепло, но опасно...", "Травянистые прерии")),
        new("sloughed-gully", IslandKind.GrandExpedition,
            (GameLanguage.English, "It's dry at least...", "Sloughed Gully"),
            (GameLanguage.Russian, "Хотя бы сухо...", "Осыпавшийся овраг")),

        new("fractured-lake", IslandKind.UniqueMap,
            (GameLanguage.English, "Reflective waters...", "The Fractured Lake"),
            (GameLanguage.Russian, "Отражающие воды...", "Расколотое озеро")),
        new("castaway", IslandKind.UniqueMap,
            (GameLanguage.English, "All that glitters...", "Castaway"),
            (GameLanguage.Russian, "Всё что блестит...", "Изгой")),
        new("untainted-paradise", IslandKind.UniqueMap,
            (GameLanguage.English, "Almost paradise.", "Untainted Paradise"),
            (GameLanguage.Russian, "Почти рай.", "Нетронутый рай")),
        new("moment-of-zen", IslandKind.UniqueMap,
            (GameLanguage.English, "A good fellow...", "Moment of Zen"),
            (GameLanguage.Russian, "Славный малый...", "Безмятежность")),

        new("jade-isles", IslandKind.Boss,
            (GameLanguage.English, "Crazed Chieftain...", "The Jade Isles"),
            (GameLanguage.Russian, "Обезумевший вождь...", "Нефритовые острова")),
        new("obscure-island", IslandKind.Boss,
            (GameLanguage.English, "Origin of the fall...", "Obscure Island"),
            (GameLanguage.Russian, "Исток падения...", "Неясный остров")),
        new("secluded-temple", IslandKind.Boss,
            (GameLanguage.English, "Stardrinker...", "Secluded Temple"),
            (GameLanguage.Russian, "Пьющий звёзды...", "Уединённый храм")),
        new("mournful-cliffside", IslandKind.Boss,
            (GameLanguage.English, "The last to fall...", "Mournful Cliffside"),
            (GameLanguage.Russian, "Последняя из павших...", "Скорбный утёс")),
        new("sprawling-jungle", IslandKind.Boss,
            (GameLanguage.English, "End of the circle...", "Sprawling Jungle"),
            (GameLanguage.Russian, "Конец круга...", "Разросшиеся джунгли")),
    ];

    private static readonly Dictionary<string, Island> ByIdIndex = All.ToDictionary(i => i.Id);

    public static Island ById(string id) =>
        Find(id) ?? throw new KeyNotFoundException($"Unknown island: {id}");

    public static Island? Find(string id) => ByIdIndex.GetValueOrDefault(id);
}
