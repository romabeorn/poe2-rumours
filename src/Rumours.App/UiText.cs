using Rumours.Core;

namespace Rumours.App;

public sealed record UiText(UiLanguage Language)
{
    private bool Ru => Language == UiLanguage.Russian;

    public string SettingValue => Ru ? "ru" : "en";

    public static UiText For(string setting) =>
        new(setting.Trim().ToLowerInvariant() == "ru" ? UiLanguage.Russian : UiLanguage.English);

    public string VerdictOpen => Ru ? "Открывать" : "Open it";
    public string VerdictSkip => Ru ? "Пропустить" : "Skip it";
    public string VerdictHover => Ru ? "Наведи на область" : "Hover an area";
    public string VerdictKeepScanning => Ru ? "Сканируй дальше" : "Keep scanning";

    public string AdviceHover(string scanKey) => Ru
        ? $"Наведи курсор на неоткрытую область и нажми {scanKey}"
        : $"Hover an uncharted area and press {scanKey}";
    public string AdviceKeepScanning(string scanKey) => Ru
        ? $"Сдвинь любой предмет в инвентаре или переключи сагу — слухи сменятся. Снова наведи и жми {scanKey}"
        : $"Move any item in your inventory or toggle a saga — the rumours will reshuffle. Hover again and press {scanKey}";
    public string AdviceOpen => Ru ? "Область стоит логбука" : "This area is worth a logbook";
    public string AdviceSkip => Ru ? "Ничего ценного — ищи другую область" : "Nothing valuable — look for another area";

    public string TooltipUnread => Ru
        ? "Слухи не разобраны — повтори скан"
        : "Couldn't read the rumours — scan again";
    public string TooltipMissing => Ru
        ? "Тултип не найден — наведи курсор на область или карту"
        : "No tooltip found — hover an area or a map";
    public string PartlyRead(int read, int total) => Ru
        ? $"Прочитано {read} из {total} слухов — повтори скан"
        : $"Read {read} of {total} rumours — scan again";
    public string NothingNew => Ru ? "Ничего нового" : "Nothing new";
    public string NewIslands(int count) => Ru ? $"Новых островов: {count}" : $"New islands: {count}";
    public string ScanFailed(string reason) => Ru ? $"Скан сорвался: {reason}" : $"Scan failed: {reason}";
    public string FullSetSeen => Ru
        ? "Слухов меньше трёх — набор показан целиком"
        : "Fewer than three rumours — this is the full set";
    public string NewMark => Ru ? "  ● новый" : "  ● new";

    public string Hint(HotkeyBindings hotkeys) => string.Join(" · ", Enum.GetValues<HotkeyAction>()
        .Where(action => hotkeys[action] != "")
        .Select(action => $"{hotkeys[action]} — {ShortActionName(action)}"));

    private string ShortActionName(HotkeyAction action) => action switch
    {
        HotkeyAction.Scan => Ru ? "скан" : "scan",
        HotkeyAction.Reset => Ru ? "новая область" : "new area",
        HotkeyAction.Toggle => Ru ? "скрыть" : "hide",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public string ActionName(HotkeyAction action) => action switch
    {
        HotkeyAction.Scan => Ru ? "Сканировать слухи" : "Scan rumours",
        HotkeyAction.Reset => Ru ? "Новая область" : "New area",
        HotkeyAction.Toggle => Ru ? "Скрыть / показать оверлей" : "Hide / show the overlay",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public string NotAssigned => Ru ? "Не назначено" : "Not assigned";
    public string KeyOrNotAssigned(string hotkey) => hotkey == "" ? NotAssigned : hotkey;
    public string KeyName(string hotkey) => hotkey == "" ? $"«{NotAssigned.ToLowerInvariant()}»" : hotkey;
    public string PressAKey => Ru ? "Нажмите клавишу…" : "Press a key…";
    public string SectionAppearance => Ru ? "Внешний вид" : "Appearance";
    public string SectionHotkeys => Ru ? "Горячие клавиши" : "Hotkeys";
    public string OverlayOpacity => Ru ? "Непрозрачность оверлея" : "Overlay opacity";
    public string BackdropShade => Ru ? "Затемнение картинки фона" : "Background image shade";
    public string HotkeysInstructions => Ru
        ? "Кликните по клавише и нажмите новую — можно с Ctrl, Alt или Shift. Esc снимает назначение."
        : "Click a key and press a new one — Ctrl, Alt and Shift combinations work too. Esc clears the binding.";
    public string HotkeyMoved(string hotkey, string from) => Ru
        ? $"{hotkey} была занята действием «{from}» — оно осталось без клавиши."
        : $"{hotkey} was bound to “{from}” — that action is now unassigned.";
    public string MapUnderCursor => Ru ? "Карта под курсором" : "Map under the cursor";

    public string TabIslands => Ru ? "Все острова" : "All islands";
    public string TabSettings => Ru ? "Настройки" : "Settings";
    public string FilterAll => Ru ? "Все" : "All";
    public string FilterExpeditions => Ru ? "Экспедиции" : "Expeditions";
    public string FilterBosses => Ru ? "Боссы" : "Bosses";
    public string FilterUniques => Ru ? "Уникальные карты" : "Unique maps";
    public string MoveOverlay => Ru ? "Переместить оверлей" : "Move the overlay";
    public string MoveOverlayDone => Ru ? "Готово — закрепить оверлей" : "Done — lock the overlay";
    public string MoveOverlayHint => Ru
        ? "Перетащите оверлей мышью, затем нажмите «Готово» в настройках"
        : "Drag the overlay with the mouse, then press “Done” in the settings";
    public string MenuOpen => Ru ? "Меню" : "Menu";

    public string CaptionScans => Ru ? "Сканов" : "Scans";
    public string CaptionStale => Ru ? "Без новых островов" : "Without new islands";
    public string CaptionIslands => Ru ? "Островов" : "Islands";
    public string CaptionExpeditions => Ru ? "Экспедиций" : "Expeditions";
    public string CaptionTier => Ru ? "Тир" : "Tier";
    public string CaptionMap => Ru ? "Карта" : "Map";
    public string CaptionKind => Ru ? "Тип" : "Type";
    public string CaptionReward => Ru ? "Что даёт" : "Reward";

    public string KindExpedition => Ru ? "Экспедиция" : "Expedition";
    public string KindBoss => Ru ? "Босс" : "Boss";
    public string KindUnique => Ru ? "Уникальная карта" : "Unique map";

    public string OpenLogsFolder => Ru ? "Открыть папку логов" : "Open the logs folder";
    public string SectionLanguage => Ru ? "Язык" : "Language";
    public string MenuExit => Ru ? "Выход" : "Exit";
    public string TrayTooltip => Ru ? "PoE2 Rumours — клик: меню" : "PoE2 Rumours — click for the menu";
    public string StartedTitle => Ru ? "PoE2 Rumours запущен" : "PoE2 Rumours is running";
    public string StartedBody(string scanKey) => Ru
        ? $"{scanKey} — скан. Меню и выход — по значку в трее."
        : $"{scanKey} — scan. Menu and exit: the tray icon.";
    public string AlreadyRunning => Ru
        ? "PoE2 Rumours уже запущен. Значок — в трее возле часов (возможно, под стрелкой «^»)."
        : "PoE2 Rumours is already running. Its icon is in the tray near the clock (possibly under the “^” arrow).";
    public string HotkeysRefused(string keys) => Ru
        ? $"Не удалось занять сочетания: {keys} — они принадлежат другой программе или записаны непонятно.\nПоменяйте их: Меню → Настройки → Горячие клавиши."
        : $"Couldn't register these hotkeys: {keys} — another program owns them, or they are misspelt.\nChange them: Menu → Settings → Hotkeys.";
    public string SettingsUnreadable(string reason) => Ru
        ? $"Файл settings.json не прочитан, взяты настройки по умолчанию.\n\n{reason}"
        : $"settings.json could not be read; default settings are used.\n\n{reason}";
    public string RatingsUnreadable(string reason) => Ru
        ? $"Ваш ratings.json не прочитан, взяты встроенные оценки.\n\n{reason}"
        : $"Your ratings.json could not be read; the built-in ratings are used.\n\n{reason}";
    public string SettingsNotSaved(string reason) => Ru ? $"Настройки не сохранены: {reason}" : $"Settings were not saved: {reason}";
    public string UnknownArguments(string arguments) => Ru
        ? $"Непонятные аргументы запуска: {arguments}"
        : $"Unknown command-line arguments: {arguments}";
    public string StartupFailed => Ru ? "PoE2 Rumours — не удалось запуститься" : "PoE2 Rumours — failed to start";
}
