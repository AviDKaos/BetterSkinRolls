# How to Fish – BetterSkinRolls mod

Client-side BepInEx 5 mod for *How to Fish*. Thunderstore package `AviDKaos-BetterSkinRolls`, GUID `avidkaos.howtofish.betterskinrolls`,
DLL/plugin folder/namespace `BetterSkinRolls`.

Shared knowledge lives in the hub folder (`..\HowtoFishPlans`): `docs/game-api.md` (game code, add findings there),
`docs/patterns.md` (layout, controls row, localization, release, conventions), `game/decompiled/` (the game's source).
Don't copy them here.

## Goal

Weapons whose skins are all unlocked stop showing up on the slot machine, with the wheel synced across the lobby.

## Design (agreed 2026-11-18: approach C)

- Only collections where **every obtainable skin** is owned are skipped (not owned skins in general: the user wants a
  smaller pool, not an easy one). Unobtainable `Rarity.Default` skins don't count.
- **Host side** (`RollRandomPatch`): prefix on `SlotMachineManager.RollRandom` swaps `SlotMachine.AvailableItems` /
  `ExcludeBoat` for the pool minus the roller's completed set, finalizer restores. Vanilla odds, cheat path and
  `SendRoll` untouched, so every client sees the same wheel, vanilla or not. Everything complete: vanilla roll.
- **Sync** (`LobbySync`): Steam lobby data, never FishNet messages. Member data `BetterSkinRolls` = `on:54,57,255` or
  `off` (published every 1 s if changed, and right after the local unlock); lobby data `BetterSkinRolls` = `on`/`off`
  = `on` from a modded host (it always filters modded players). Missing = vanilla host.
- **Vanilla host** (`RollPatch`, approach A): prefix on `SlotMachine.Roll` when you rolled, aren't the server and the
  host is vanilla: completed slots become a random incomplete collection with the same rarity, arrays edited in place
  (SlotMachineSkinNames' `SetIndex` postfix and the unlock see the new prize). Others see the original roll.
- Toggles: each player's own (Enabled, Strike), in a 5th Options tab "Skin Rolls" (`GameMenus`).
  The host's lobby-wide switch was removed on 2026-11-18 (user: only modded players benefit, and they can turn it
  off themselves). The "Local fallback" switch was removed the same day: with Enabled on, a vanilla host always gets
  the local change (turning Enabled off covers "same wheel as everyone").
- Extra: `UpdatePrizesText` postfix strikes completed prizes locally.

## Layout

- `BetterSkinRolls/` – plugin (netstandard2.1). `Plugin.cs`: config, Harmony, lobby sync tick. `Patches.cs`,
  `LobbySync.cs`, `SkinProgress.cs` (completed collections from the local save).
- `lib/`, `dist/` – gitignored. `tools/`: `r2modman_import.py`, `make_icon.py`, `game_strings.py`.

## Build

`dotnet build` (in `BetterSkinRolls/`) installs into the r2modman Dev profile as "BetterSkinRolls (Dev)". Release:
`htf release BetterSkinRolls X.Y.Z`, then `htf publicar` after the user's upload (see `docs/patterns.md` in the hub).

## Status

- Created with `htf novo`. 1.0.0 tested in game 2026-11-18: solo, modded host + modded client (both ways), vanilla
  host (local change), vanilla client, Skip off.
- **1.0.0 published 2026-11-18**: https://thunderstore.io/c/how-to-fish/p/AviDKaos/BetterSkinRolls/ ,
  GitHub release https://github.com/AviDKaos/BetterSkinRolls/releases/tag/v1.0.0 . Not in the modpack yet (hub task).
