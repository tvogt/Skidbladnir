# Skíðblaðnir

**Freyr's folding ship for Valheim: a longship you can fold up, carry through portals and unfold at any shore.**

> Skíðblaðnir is the best of ships. It was made by the dwarves, the sons of Ívaldi, and given to Freyr. It is big enough to carry all the Æsir with their weapons and war gear, and as soon as its sail is raised it has a fair wind, wherever it is bound. When it is not needed at sea, it can be folded like a cloth and carried in a pouch.
> (after Snorri Sturluson, *Gylfaginning* 43)

Getting a ship to a far coast usually means tearing it down, carrying the materials through a portal and building it again. With Skíðblaðnir you fold the ship, walk through the portal and unfold it on the other side.

This mod is made intentionally lore- and vanilla-friendly. It introduces no new workbenches or materials, uses no custom models or textures or hotkeys. It’s also based on actual Norse mythology, bringing yet another small piece of this fascinating culture into an amazing game.


## Features

- **Craft it** at a **level 3 Forge** (Forge Cooler + Anvils: dwarven work, more advanced than a ship built on the beach): the materials of a longship plus **2 Serpent trophies**. It weighs 2.
- **Unfold it**: equip the folded ship like the hammer and place it. The game's own longship placement rules apply (water, space, wards, no-build zones). No workbench needed.
- **Fold it**: use the hammer's remove action on the ship. The hold must be empty and closed, and nobody may be aboard. Ordinary ships can't be removed with the hammer; Skíðblaðnir is the exception, and you get the folded ship back.
- **Level and health are kept**: a damaged ship stays damaged when folded (shown as the item's durability). Repair it at the Forge like any item, or unfolded with the hammer near a workbench like any longship.
- **Level 2, the fair wind**: upgrade it at a **level 5 Forge** (four extensions, iron age) with the **Moder trophy**. A level-2 Skíðblaðnir always has the wind behind it while someone is aboard, like Moder's power but only for this ship. Since the Moder trophy usually goes to the sacrificial stones, this means killing Moder a second time.

Everything else is a normal item: anyone can craft one, it goes through portals, and it lands in your tombstone when you die.

## How it works

- The unfolded ship is an ordinary vanilla longship with a marker for its level. If you remove the mod, unfolded ships stay as normal longships. **Folded ships in inventories and chests are lost**, because the item no longer exists without the mod. Unfold them first.
- Only Skíðblaðnir ships fold. Ordinary ships still can't be removed with the hammer and have to be destroyed as in vanilla.
- The hold must be empty to fold, so the folded ship cannot carry ore or other non-teleportable cargo through portals.

## Configuration

`BepInEx/config/local.Skidbladnir.cfg`. All settings are synced from the server; with `LockConfiguration` on, only admins can change them.

| Section | Setting | Default | Meaning |
|---|---|---|---|
| 1 - Recipe | CraftingStation | `forge` | Prefab name of the station for crafting, upgrading and repairing |
| 1 - Recipe | MinStationLevel | `3` | Station level for crafting and repairing |
| 1 - Recipe | UpgradeStationLevel | `5` | Station level for the upgrade to level 2 |
| 1 - Recipe | LongshipMaterials | `true` | Include the longship's materials (read from the game, so mods that change them are followed) |
| 1 - Recipe | ExtraMaterials | `TrophySerpent:2` | Further materials, `PrefabName:Amount, ...` |
| 1 - Recipe | UpgradeMaterials | `TrophyDragonQueen:1` | Materials for the upgrade to level 2 only |
| 2 - Item | Weight | `2` | Weight of the folded ship |
| 3 - Fair wind | TailwindLevel | `2` | Item level that has the fair wind; `3` turns it off |

## Multiplayer

The mod must be installed on the server and on every client, in the same version. Clients without it are refused by the server.

## Installation

Download: https://valheim.hexium.gg/mods/Lemuria/Skidbladnir

With a mod manager: import the zip. By hand: put `Skidbladnir.dll` into `BepInEx/plugins/`. Requires [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

## Building from source

Requires the .NET SDK (6 or newer). The project targets .NET Framework 4.8 and builds on Windows, Linux and macOS without Mono.

The game assemblies are not part of this repository. Copy them into a `refs` folder next to the repository folder, or point `RefsDir` at another folder:

```
refs/Managed/   <- Valheim/valheim_Data/Managed/*.dll
refs/core/      <- Valheim/BepInEx/core/*.dll
```

```sh
dotnet build -c Release                         # refs next to the repository folder
dotnet build -c Release -p:RefsDir=/path/refs/  # or anywhere else (trailing slash)
./package.sh                                    # build + dist/Lemuria-Skidbladnir-<version>.zip
```

The version is in `Plugin.cs` and `package/manifest.json`; `package.sh` checks that they match.

## Source and bug reports

https://github.com/tvogt/Skidbladnir. Please report problems there as issues, with your `BepInEx/LogOutput.log`.

## Credits and licence

MIT licence, see [LICENSE](LICENSE). `ServerSync/ConfigSync.cs` is [ServerSync](https://github.com/blaxxun-boop/ServerSync) by blaxxun-boop, included unmodified under its own licence (`ServerSync/LICENSE.txt`).
