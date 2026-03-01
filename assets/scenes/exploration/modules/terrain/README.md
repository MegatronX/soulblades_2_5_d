# Terrain Module Kit (Blockout)

These are intentionally simple, reusable blockout modules.

## Modules
- `TerrainPlateauModule.tscn` (`8m x 8m` flat shelf)
- `TerrainPlateauSmallModule.tscn` (`4m x 4m` flat shelf)
- `TerrainCliffFaceModule.tscn` (`8m` cliff face segment)
- `TerrainCliffOuterCornerModule.tscn` (L-shaped outer corner cliff)
- `TerrainCliffInnerCornerModule.tscn` (L-shaped inner corner cliff)
- `TerrainRampModule.tscn` (inclined transition)
- `TerrainStairModule.tscn` (4-step elevation transition)
- `BridgeSegmentModule.tscn` (`8m` bridge deck segment)
- `BridgeSegmentShortModule.tscn` (`4m` bridge deck segment)
- `BridgeSegmentLongModule.tscn` (`12m` bridge deck segment)
- `RiverBankEdgeModule.tscn` (bank lip + wall edge)
- `RiverBankCurveModule.tscn` (bank curve approximation)

## Usage
1. Instance modules into your map scene.
2. Use `SnapPoints` markers for manual alignment.
3. Keep terrain collision in your map `Collision` hierarchy, or use module collision directly during blockout.
4. Replace module visuals with polished meshes/sprite cards later while preserving transforms/collision intent.

## Sandbox
- `res://assets/scenes/exploration/modules/sandboxes/TerrainModuleKitSandboxScene.tscn`
