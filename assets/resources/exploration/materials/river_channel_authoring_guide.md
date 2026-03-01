# River Channel Authoring Guide

Use `res://assets/scenes/exploration/props/RiverChannel.tscn` for rivers that need:
- separate terrain vs river textures/materials
- visible channel depth
- either smooth banks or rocky gorge walls

## Core setup

1. Instance `RiverChannel.tscn` under your map `EnvironmentGeometry`.
2. Set channel transform (position/rotation) to align the river corridor.
3. Assign materials:
- `TerrainMaterial` for the surrounding bank/ground strips
- `WaterMaterial` for animated river surface (typically `forest_river_flow_material.tres`)
- `BedMaterial` for the river bottom
- `SmoothBankMaterial` and `GorgeWallMaterial` for side geometry

## Bank styles

- `BankStyle = SmoothBanks`
  - Use for low-relief rivers near map ground level.
  - Tune `SmoothBankRun` and `WaterSurfaceY` for gentle slopes.

- `BankStyle = RockyGorge`
  - Use for carved channels / cliff-like river sides.
  - Tune `GorgeWallThickness`, `GorgeWallHeight`, and `ChannelDepth`.

## Ground disruption and depth

To show true depth, avoid rendering a full unbroken ground mesh through the river area.
Recommended options:

1. Split map ground visuals into sections above/below the river corridor.
2. Or disable the old full ground visual and let `RiverChannel` terrain strips (`TerrainPositiveWidth`, `TerrainNegativeWidth`) carry the bank visuals.

Keep floor collision separate from visuals if needed.

## Collision

- `GenerateCollision` can build simple floor/bank/wall colliders from the same geometry.
- Enable `BlockRiverCrossing` if river should be non-traversable.

If your map already has custom collision, leave `GenerateCollision = false`.

## Curves and bends

`RiverChannel` is straight by default. For curved rivers:
- chain multiple channel instances with slight rotation offsets, or
- use a custom curved mesh + the flow-map support in `river_flow.gdshader`.
