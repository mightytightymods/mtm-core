# MTM Difficulty
Global control over creature and boss health and damage, including star bonuses.

## Features
* Server-authoritative
* Dynamic, in-game updates via BepInEx configuration manager
* Simple configuration
* Minimalist scope and design

## Configuration
Configuration follows basic BepInEx patterns that are mostly self-explanatory. The [official Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/) is excellent and greatly speeds up iterative adjustments.

### Per-star level adjustments

Per-star health scaling is direct enough, since a star by default adds +100%. Setting `PerStarHealthMultiplier` to `0.8` would result in each star adding +80% more health.

Per-star damage scaling works the same way, but the default adds +50% damage per star instead. Setting `PerStarDamageMultiplier` to 0.8 would result in each star adding +40% for damage; i.e., a 2-star creature would do a total of 80% more damage.

### Example

```
[Bosses]
BossHealthMultiplier = 1
BossDamageMultiplier = 1

[Creatures]
HealthMultiplier = 1.25
DamageMultiplier = 1.1
PerStarHealthMultiplier = 0.9
PerStarDamageMultiplier = 0.82
```
The base multiplier and the per-star multiplier work together.

Applying the example configuration changes above to a *Fuling* would result in:

* Health: `175 × 1.25 × (1 + stars × 1.0 × 0.9)`
* Damage: `85 × 1.1 × (1 + stars × 0.5 × 0.82)`


| Star Level | Vanilla Health | Modded Health | Vanilla Damage | Modded Damage |
|---|---|---|---|---|
| 0-star | 175 | 219 | 85 | 94 |
| 1-star | 350 | 416 | 128 | 132 |
| 2-star | 525 | 613 | 170 | 170 || 2-star | 525 | 170 | 613 | 170 |


The example configuration keeps 2-star damage at vanilla levels, while making 0 and 1-star slightly more damaging.

## Interactions and Compatability
This mod is unlikely to play nicely with other difficulty adjusting mods, such as `CreatureLevelAndLootControl` or `StarLevelSystem`.

This mod acts **on top** of Valheim's World Modifiers difficulty system, respecting and **multiplying** those values. Since it applies the configured multipliers after the World Modifiers have been applied, you can, for example, use this mod to apply a second set of adjustments that would result in a "Hard" setting that was harder or easier.

Health multipliers are applied when a creature is spawned, or reloaded. If you were fighting a boss and wanted to adjust its health using the BepInEx configuraiton manager, you would first need to leave the immediate area around the boss to force the game to unload the boss, update the boss config, and then approach the re-loaded (and freshly updated) boss. Exiting the game, manually editing the .cfg, and then restarting would have the same effect.

Damage multipliers are applied right before a player takes damage. Changes to the damage config are applied instantly.

## Installation (manual)
I highly recommend using a mod manager such as [r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/) or [Gale](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/).

Manual installation will require getting BepInEx and Jotunn [working first](https://valheim-modding.github.io/Jotunn/guides/installation.html), then extracting the contents of the mod into a subfolder in the \<BepInEx Path>\plugins directory.

## AI Disclosure
Most of the code was generated using a free claude.ai account. In no sense was this mod "vibe coded."
