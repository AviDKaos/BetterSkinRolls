# BetterSkinRolls

The casino slot machine stops offering weapons whose skins you have all collected. Your rolls only land on collections
you haven't finished, so the last skins of a weapon come sooner, without making skins you already own impossible
(a weapon you haven't finished can still give you a duplicate, as in the vanilla game).

## How it works in multiplayer

The host is the one who rolls the wheel, so:

- **Host with the mod**: when a player who also has the mod rolls, the host builds the wheel without that player's
  completed weapons. Everyone in the lobby sees the same wheel and the same prize, **even players without the mod**.
- **Host without the mod**: your own rolls are changed on your side only. You still get a skin of a weapon you
  haven't finished, but the other players see the original wheel and prize. If you'd rather see the same wheel as
  everyone, turn *Skip completed weapons* off for that game.
- Players without the mod always get vanilla rolls.

The mod shares which weapons you completed through Steam lobby data, so vanilla players never receive anything new.

## Features

- Completed weapons (and the boat) never show up on the wheel when you roll.
- Odds stay vanilla: 7 common, 2 rare and 1 legendary slot, then a random winner, spread over the weapons you haven't
  finished on that island.
- Skins that can't be obtained (for example Assault Rifle Orange/Blue, Fishing Rod Gold, Pistol Missing, Boat Fire)
  don't count: a weapon is complete when every skin the machine can give is yours.
- If every prize of an island is complete, the roll is vanilla.
- The machine's "Prizes:" list crosses out the weapons you completed (only on your screen).

## Settings

In game:

- **Options > Skin Rolls** (your own settings): *Skip completed weapons* and *Cross out completed weapons in the prize
  list*, each with a short description. There's no host
  setting: every player decides for their own rolls.

The same settings are in r2modman's Config editor (`avidkaos.howtofish.betterskinrolls.cfg`):

| Setting | Default | What it does |
|---|---|---|
| General > Enabled | on | Your rolls leave out weapons whose skins you've all unlocked. |
| General > Strike completed prizes | on | Cross out your completed weapons in the machine's "Prizes:" list. |

## Compatibility

Works with the other AviDKaos mods, including SlotMachineSkinNames (the names shown are the filtered prizes) and
VanillaLikeSkinChecklist. Other mods that also change slot machine rolls (for example Fresh Wheel Spins) may conflict:
use only one of them.
