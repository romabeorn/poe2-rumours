# Contributing

Thanks for wanting to help. Read [ARCHITECTURE.md](ARCHITECTURE.md) first: it explains how the pieces fit and
why they are shaped the way they are.

## Ground rules

- **The tool only looks at the screen.** Nothing that reads game memory, touches game files or sends input to
  the game will be accepted, however convenient.
- **Domain rules live in `Rumours.Core`** and come with tests. The WPF layer turns state into text and
  colours; it does not decide anything about the game.
- **Facts apart from opinions.** Rumour and map texts are game data and belong in `IslandCatalog`. Tiers,
  rewards and thresholds are opinion and belong in `data/ratings.json`.
- **English everywhere** — code, logs, exception messages, documentation, commit messages. Other languages
  appear only as data (localised UI strings, game client texts).
- **No comments in the source.** Express intent through names and structure; record the reason behind a
  non-obvious decision in ARCHITECTURE.md.
- **Delete what is no longer used** rather than leaving it behind.

## Workflow

1. Open an issue before starting anything larger than a fix, so the approach can be agreed on first.
2. Fork, create a branch, make the change together with its tests.
3. Run `dotnet test` — CI runs the same on every pull request.
4. Keep a pull request to one topic and describe what changed and why.

## Reporting a recognition problem

Set `"saveShots": "failed"` in `%LOCALAPPDATA%\PoE2 Rumours\settings.json`, reproduce the problem and attach
the saved screenshot together with the matching part of `logs\scans.log`. Check the screenshot first — it
shows a square around your cursor and may include chat or other things you would rather crop out.

`tools/OcrProbe` runs saved screenshots through the same scanner as the overlay, so a reported failure can be
reproduced exactly and turned into a test.

## Adding a game client language

1. Add a value to `GameLanguage` and a line to `GameLocales`: the Windows OCR language tag and the three
   tooltip anchor texts, lower-case letters only.
2. Add the rumour and map texts of all islands for that language to `IslandCatalog`.
3. Add tests with strings as Windows OCR actually reads them — take them from `scans.log`.

Ideographic languages will also need their own similarity thresholds in `RumourMatcher`; see the caveat in
ARCHITECTURE.md.

## Updating ratings for a new patch

Edit `data/ratings.json`. Every island in `IslandCatalog` must have a rating; texts may be a plain string or a
`{ "ru": …, "en": … }` pair.
