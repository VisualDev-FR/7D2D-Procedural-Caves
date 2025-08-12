# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.1.1] - 2025-08-12

### Fixed
- Fixed zombies not spawning at all when setting `enableCaveSpawn` is false
- Fixed calls to PrefabEditModeManager from the GameManager (v2.2b3 compatibility)

## [2.1.0] - 2025-07-29

### Added
- Added russian localization (thanks https://github.com/mpustovoi !)

### Changed
- Improved cave spawning during bloodmoon when player is inside a cave prefab

### Fixed
- Fixed MethodNotFound Exception raised during bloodmoon

## [2.0.1] - 2025-07-12

### Fixed
- Fixed excessive high-tier loot spawning containers due to obsolete harmony patch.
- cave-prefabs: Fixed the missing prefabs included in the base mod.

## [2.0.0] - 2025-06-29

### Added
- New parameter in ModConfig.xml: prefabScoreMultiplier
- New parameter in ModConfig.xml: zombieSpawnMarginDeep
- Added compatibility for v2.0.0295

### Changed
- Check if the player is in cave instead of looking only if he's under terrain (affecting zombies spawn)
- Removed the custom wilderness prefab spawner

### Fixed
- cave-prefabs: fix yOffset on tonio_entrance_03
- cave-prefabs: fix poi bounding size of tonio_filler_26 (marker issues)
- cave-entities: fix AI of zombieFatCop

## [0.0.4] - 2025-03-29

### Added
- Support for custom decoration in each biome through blockplaceholders.xml

### Changed
- brings back worldglobal.xml from cavelights
- brings back moon light modifier from cavelights
- prevents flashlights disabling during cave horde game event

### Fixed
- (mod-utils)     Fix case sensitivity of ModConfig.xml (issues on linux, see [this commit](https://github.com/VisualDev-FR/7D2D-mod-utils/commit/34f348a1e697a848c4c9aa498be65e2bc08528ba))
- (cave-entities) Fix assembly reference to EAIEatBlock
- (cave-lights)   Fix NullReferenceException if no Light component can be found on lightTransform (see [this commit](https://github.com/VisualDev-FR/7D2D-Powered-flashights/commit/6d8ef6e3e4012b3a6a105d4e05343658bd132ee6))

## [0.0.3] - 2025-02-22

### Changed
- (cavelights) remove custom progression to unlock items

### Fixed
- Fixed vanilla LootFromXml.cs to prevent crashing if a lootgroop is not found

## [0.0.2] - 2025-02-21

## Added
- Added 7D2D-mod-utils (shared library)
- Added `TheDescent/ModConfig.xml`
- Added `TheDescent-cave-lights/ModConfig.xml`
- Added the possibility to disable cave bloodmoons from `TheDescent/ModConfig.xml`

### Changed
- Removed `H_SkyManager` (moved to cave-lights)
- Removed `worldglobal.xml` (moved to cave-lights)
- Removed `aa_battery` from `loot.xml` (moved to cave-lights)
- Removed `roadFlare` from `loot.xml` (moved to cave-assets)
- Removed collectible items (moved to cave-assets)
- Removed `EAIEatBlock` (moved to cave-entities)
- Prevented traders from offering quests at underground prefabs
- Prevented spawning of cave trader prefabs during RWG

### Fixed
- Prevented `NullReferenceException` if a prefab fails to load
- (caveLights) Prevented `NullReferenceException` when a `lightSource`'s transform was not found
- (caveLights) Fixed blocked `modArmorNightVision` turning on


## [0.0.1] - 2025-02-15

- First release


[unreleased]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/master...unreleased
[2.1.1]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/2.1.0...2.1.1
[2.1.0]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/2.0.1...2.1.0
[2.0.1]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/2.0.0...2.0.1
[2.0.0]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.4...2.0.0
[0.0.4]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.3...0.0.4
[0.0.3]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.2...0.0.3
[0.0.2]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.1...0.0.2
[0.0.1]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/tree/0.0.1
