# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.4] - 2024-02-22

### Added
- Support for custom decoration in each biome through blockplaceholders.xml

### Fixed
- (cave-entities) Fix assembly reference to EAIEatBlock
- (cave-lights)   Fix NullReferenceException if no Light component can be found on lightTransform (see [this commit](https://github.com/VisualDev-FR/7D2D-Powered-flashights/commit/6d8ef6e3e4012b3a6a105d4e05343658bd132ee6))

## [0.0.3] - 2024-02-22

### Changed
- (cavelights) remove custom progression to unlock items

### Fixed
- Fixed vanilla LootFromXml.cs to prevent crashing if a lootgroop is not found

## [0.0.2] - 2024-02-21

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


## [0.0.1] - 2024-02-15

- First release


[unreleased]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/master...unreleased
[0.0.4]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.3...0.0.4
[0.0.3]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.2...0.0.3
[0.0.2]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.1...0.0.2
[0.0.1]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/tree/0.0.1
