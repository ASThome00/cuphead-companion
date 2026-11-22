# Cuphead Companion

A vanilla-friendly helper mod for Cuphead. It adds information and small conveniences in the game's
own visual style. It does not change combat, difficulty, grades or your save file, and every feature
can be turned off on its own.

## Features

| Feature | What it does | Default |
|---|---|---|
| Map level cards | Walk up to a level on the world map and a card shows your best grade, best time, difficulty beaten, and whether you've beaten it as Ms. Chalice or with the Cursed Relic. If the relic is still charging, it shows how many points the level is worth. The King of Games ladder shows all five champions and the gauntlet. | On |
| Checklist icons | In the equip menu's checklist, an Astral Cookie icon marks levels beaten as Ms. Chalice and a relic icon marks levels beaten with the Cursed Relic. Levels that can still charge the relic show their point value. | On |
| Progress card | Beside the checklist: relic state and power toward the Divine Relic, hidden coins found, plus progress to the 2-Strip filter, the black and white filter and the Ms. Chalice recolor. | On |
| Reflecting pool record | Stand at the reflecting pool and a card shows your totals: deaths, best parry streak, bosses beaten, A ranks, Expert wins, completion, wishes left, and wins per character when the win log is on. | On |
| Results top time | The results screen shows your previous top time under the top grade, and says when you beat it. | On |
| Skip studio intro | Skips the Studio MDHR logo and goes straight to the title screen. | On |
| Fast retry | Press confirm as soon as "YOU DIED" appears to retry at once, without waiting for the death card. Changes pacing, so it is opt-in. | Off |
| Win log | Appends one line of JSON per win (level, grade, time, difficulty, who was playing) to a local file for your own stats tools. Nothing is sent anywhere. | Off |

## Requirements

- Cuphead for Windows (Steam), 64-bit. Built and tested with The Delicious Last Course installed.
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (the `win_x64` build of 5.4.x).

## Install

1. Install BepInEx 5: unzip `BepInEx_win_x64_5.4.x.zip` into the Cuphead folder (the one containing
   `Cuphead.exe`), then run the game once and close it.
2. Unzip this mod into the same Cuphead folder. You should end up with
   `BepInEx\plugins\CupheadCompanion\CupheadCompanion.dll`.
3. Start the game.

To find the Cuphead folder in Steam: right-click Cuphead, then Manage, then Browse local files.

## Settings

Run the game once with the mod installed, then edit
`BepInEx\config\cuphead.companion.cfg` in the Cuphead folder and restart the game.

| Setting | Section | Default |
|---|---|---|
| `MapLevelPopup` | Info | true |
| `ChecklistIcons` | Info | true |
| `ChecklistProgressPanel` | Info | true |
| `ResultsTopTime` | Info | true |
| `PoolRecordCard` | Info | true |
| `SkipStudioIntro` | Helpers | true |
| `FastRetry` | Helpers | false |
| `LogWins` | Tracker | false |

With `LogWins` on, wins are written to `%LOCALAPPDATA%\CupheadCompanion\wins.jsonl`.

## Uninstall

Delete the `BepInEx\plugins\CupheadCompanion` folder. To remove the mod loader as well, delete the
`BepInEx` folder, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt` from
the Cuphead folder.

When a Run 'n Gun stops being a pacifist run, the mod notes which enemy died in
`BepInEx\LogOutput.log`, which helps when a P grade doesn't show up.

## Known limits

- Card labels are in English in every game language. Level names follow the game's language.
  Languages other than English are untested.
- Playing without The Delicious Last Course is untested.
- The secret graveyard boss has no map card once it is beaten, because the game stops showing its
  prompt there.
- The win log's per-character data starts when the mod is installed; earlier wins can't be recovered.
- A game update can break the mod until it is rebuilt.

## Building from source

Requires the .NET SDK and a Cuphead install with BepInEx 5 in it.

1. Tell the build where Cuphead is: copy `CupheadCompanion\GameDir.props.example` to
   `CupheadCompanion\GameDir.props` and edit the path, or set the `CUPHEAD_DIR` environment variable.
2. Run `dotnet build CupheadCompanion/CupheadCompanion.csproj -c Release`.

The build references the game's own libraries from your install and copies the finished plugin into
`BepInEx\plugins\CupheadCompanion` (close the game first). Pass `-p:DeployToGame=false` to skip the copy.

This repository contains no game code or assets. The mod reuses the game's own fonts, sprites and
icons at runtime from your installed copy.

Cuphead is a trademark of StudioMDHR Entertainment Inc. This is an unofficial fan-made mod and is
not affiliated with or endorsed by StudioMDHR.

## License

MIT. See `LICENSE`.
