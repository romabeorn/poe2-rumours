# Architecture

An overlay for farming Expedition in Path of Exile 2. On a hotkey it reads the "Uncharted Waters" tooltip
off the screen, accumulates the island rumours of an ocean area and tells whether the area is worth a logbook.

## Principles

**The application only looks at the screen.** It does not read the game's memory and sends no input to it:
one hotkey press is one screenshot. Reshuffling the rumours (moving an item in the inventory) is done by the
player; automating it would break GGG's rules.

**Domain rules live in the core.** Rumour matching, tooltip parsing, accumulating the set of an area and the
verdict are in `Rumours.Core`, with no Windows dependencies and with tests. When to stop reshuffling is the
player's call: the core only counts the scans that brought no new islands. The overlay merely turns state
into text and colours.

**Facts apart from opinions.** The "rumour → map" mapping is game client data; it is baked into
`IslandCatalog` and changes only with a patch. Tiers, rewards, notes and verdict thresholds are opinion: they
live in `data/ratings.json`, which is embedded into the exe, and a player can override them with their own
`ratings.json` without a rebuild.

**OCR does not have to be accurate.** There are only 20 possible rumours, so a line is compared with each one
by Levenshtein distance and accepted only when the nearest candidate is close and clearly ahead of the runner-up.
Windows OCR misreads the handwritten rumour font in a consistent way, so the letters it confuses are folded
into one before comparing.

**A short list is information; a half-read one is not.** The game shows up to three rumours; fewer than
three means the whole set is shown. To tell that apart from an OCR failure the parser requires an anchor —
the "Island Rumours" header or, when it was not read, the "Use a Logbook to chart the area" line above it.

**A rumour is whatever lies in a slot.** The tooltip always has the same height: three slots under the
header, the unused ones left as blank parchment. The slot positions, as fractions of the distance between the
header and "Requires:", were measured from the scan log and do not depend on the game's UI scale. A garbled
header does not fall into a slot; a slot with ink but no recognised line is a missed rumour
(`GreyImage.HasInk`); a rumour outside the slots means the layout has changed (a patch), and such a reading is
not trusted. Without "Requires:" the layout is unknown and the reading is never considered complete
(`TooltipScan.Complete`).

**One screenshot is read in several ways.** The scanner grabs a square around the cursor, and the reading
order belongs to the core (`ScanPlan`): preprocessing variants in turn (colour ×2, contrast ×3, colour ×3)
until the combined result is complete. `TooltipScan.Combine` merges readings within one language; a complete
reading beats any union, and four different rumours in three slots are a reason to ask for a rescan rather
than to truncate. To the core a recogniser is the `IShotReader` interface, so the whole order is covered by
tests without Windows OCR.

**The release is one file, and it writes only to its own folder.** Everything the program stores lives in
`%LOCALAPPDATA%\PoE2 Rumours` (`AppPaths`): settings, the optional rating override and background picture,
logs. Nothing is written next to the exe. The `POE2RUMOURS_DATA` environment variable points the program at
another folder, which is how documentation screenshots are rendered from a clean state.

**Every failure leaves evidence.** `logs/scans.log` keeps everything OCR saw in every pass.
Screenshots are not saved by default; with `"saveShots": "failed"` (or `"all"`) in `settings.json` they are, and
`tools/OcrProbe` runs them through the same scanner, so a failure can be reproduced and turned into a test.

**The game client language is detected, not configured.** The parser tries the anchors of every language and
the scan plan tries the recogniser of every language, starting with the one that worked last time. The
overlay shows names in the client's language.

**A new client language is data, not code.** Three things are needed: a `GameLanguage` value, a line in
`GameLocales` (the Windows recogniser tag and the tooltip anchor texts) and the island texts in
`IslandCatalog`. Matching, parsing, the scanner and the overlay iterate those lists and never name a language
themselves. Caveat: the similarity thresholds and the letter folding in `RumourMatcher` are tuned for Latin and
Cyrillic; ideographic languages with their short strings will need their own.

## Project map

```
src/Rumours.Core      island catalogue and client languages, matching, tooltip parsing, scan plan,
                      area survey, ratings
src/Rumours.Capture   screen capture and the OCR built into Windows (Windows.Media.Ocr)
src/Rumours.App       WPF: click-through overlay, menu window (island reference and settings),
                      shared look (Theme.xaml), UI localisation, global hotkeys, tray
tools/OcrProbe        checks recognition on saved screenshots
tests/                core tests and application-layer tests (xUnit), run with: dotnet test
data/                 ratings.json, embedded into the exe
docs/                 screenshots for the README, rendered with --preview*
```

Dependencies point one way: `App → Capture → Core`.

## Data flow

```
hotkey ─► ScanController ─► TooltipScanner: ScreenGrabber (a square around the cursor)
       ─► ScanPlan: OcrReader × preprocessing ─► TooltipParser ─► TooltipScan.Combine
       │     └─ no rumour tooltip ─► MapNameMatcher (the name of an opened map)
       ─► ScanOutcome ─► OverlayViewModel.Apply / Inspect ─► AreaSurvey.AddScan ─► Verdict
                     └─► ScanLog (logs/scans.log, optional screenshots; errors.log)
```

The overlay window is excluded from screen capture (`WDA_EXCLUDEFROMCAPTURE`), so the application's own text
never reaches OCR. For the same reason its layout cannot be captured with a screenshot — the `--preview*`
modes (`OverlayPreview`) render the windows to a file instead.

## Interface

The overlay shows one thing at a time: the survey of an area or the map under the cursor; a scan of one kind
resets the other. The program has a single ordinary window, `MenuWindow`, with the "All islands" and
"Settings" tabs; its state and texts live in `MenuViewModel`, which decides nothing and reports the player's
intentions as events. `App` carries them out — it is a composition root with no logic of its own: scanning is
run by `ScanController`, the tray by `TrayMenu`. The island table (`IslandTable` + `IslandRow`) is shared by
the overlay and the reference. The look is gathered in `Theme.xaml`: no game art ships with the program, and
the optional background picture is supplied by the player as `theme\panel.png`. Colours are defined only in
`Theme.xaml`; code fetches them from there by key.

The UI language is English by default and can be switched to Russian in the settings; it is independent of the
game client language.

A player's `ratings.json` that cannot be read falls back to the built-in ratings with a warning.

`settings.json` belongs to the program (`SettingsStore`): it is written whole, through a temporary file and
with a delay, so that a slider does not cause hundreds of writes. It is read leniently — comments, a trailing
comma, out-of-range values; an unreadable file means default settings and a warning, not a refusal to start.
After start-up an error in any handler goes to `logs/errors.log` and the status line, and the overlay keeps
working.

## Conventions

Code, log output, exception messages and documentation are in English. Russian appears only where it is
data: the Russian UI strings, the Russian game client texts and the Russian reward descriptions in
`ratings.json`. The source has no comments; the reasons behind decisions are recorded in this file.
