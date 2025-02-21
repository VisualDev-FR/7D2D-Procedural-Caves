# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.2] - 2024-02-21

### Added
- Added the possibility to disable cave bloodmoons from `ModConfig.xml`
- Added `ModConfig.xml`

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
[0.0.2]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/compare/0.0.1...0.0.2
[0.0.1]: https://github.com/VisualDev-FR/7D2D-Procedural-Caves/tree/0.0.1
