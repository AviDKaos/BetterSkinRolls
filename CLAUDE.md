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
  from the host (Host > Lobby filter). Missing = vanilla host.
- **Local fallback** (`RollPatch`, approach A): prefix on `SlotMachine.Roll` when you rolled, aren't the server and the
  host is vanilla: completed slots become a random incomplete collection with the same rarity, arrays edited in place
  (SlotMachineSkinNames' `SetIndex` postfix and the unlock see the new prize). Others see the original roll.
- Toggles: global by the host (Lobby filter) + each player's own Enabled / Local fallback. No per-player host list.
- Extra: `UpdatePrizesText` postfix strikes completed prizes locally.

## Layout

- `BetterSkinRolls/` – plugin (netstandard2.1). `Plugin.cs`: config, Harmony, lobby sync tick. `Patches.cs`,
  `LobbySync.cs`, `SkinProgress.cs` (completed collections from the local save).
- `lib/`, `dist/` – gitignored. `tools/`: `r2modman_import.py`, `make_icon.py`, `game_strings.py`.

## Build

`dotnet build` (in `BetterSkinRolls/`) installs into the r2modman Dev profile as "BetterSkinRolls (Dev)". Release:
`htf release BetterSkinRolls X.Y.Z`, then `htf publicar` after the user's upload (see `docs/patterns.md` in the hub).

## Status

- Created with `htf novo`. 0.1.0 implemented, waiting for the in-game test (solo, modded host, vanilla host).
