# HD-2D Vertical Slice V2 Workflow

This project can absolutely support the style in your reference screenshots:
- layered 3D terrain/geometry
- sprite-driven characters/props
- tile/sprite overlays for detail
- cinematic camera + post effects

The right approach is **not** to keep stretching the current prototype scene forever.
Use a controlled migration to a new `VerticalSliceV2` scene while keeping the current slice as a regression baseline.

## Recommendation: Rebuild, but in-place migration style

Do **not** delete the current `ForestExplorationVerticalSlice.tscn`.
Create a new V2 scene and port systems in this order:
1. Core scene stack (camera, environment, atmosphere, weather, music, encounters).
2. Terrain blockout + traversal collisions.
3. River path authoring (`RiverPathChannel`) and water materials.
4. Prop layering + sprite lighting.
5. Interaction layer (NPC/chest/zones/transitions).
6. Final polish (fog shafts, bloom, ambient wildlife, prop lights).

This keeps momentum while reducing risk.

## Production Scene Stack (Node Layout)

Use this consistent hierarchy in each exploration map:

1. `MapRoot` (scene controller, spawn routing, map id)
2. `Systems`
   - `ExplorationCameraRig`
   - `WorldEnvironment`
   - `SceneVisualDirector`
   - `SceneAtmosphereSystem`
   - `WeatherSystem`
   - `AmbientPropSystem`
   - `ExplorationMusicController`
   - `EncounterManager`
3. `Geometry`
   - `TerrainBase` (walkable shelves/terraces)
   - `TerrainCliffs` (vertical faces/ledges)
   - `Water` (river channels, waterfalls, foam planes)
   - `BridgesAndOverpasses`
4. `Collision`
   - `WalkableBodies`
   - `Blockers` (base/trunk collisions, boundary blockers)
   - `LayerSwitchVolumes`
5. `Gameplay`
   - `SpawnPoints`
   - `NPCs`
   - `Treasure`
   - `InteractionZones`
   - `SceneTriggers`
6. `SetDress`
   - `ForegroundProps`
   - `MidgroundProps`
   - `BackgroundCards` (mountain/sky sprite cards)
7. `LightingProps`
   - emissive mushrooms/crystals/lamps

## Terrain + Layer Workflow

Build maps in three passes:

1. Blockout pass (graybox)
- Build terraces and cliffs with simple meshes first.
- Validate traversal and camera framing before detail.
- Add only trunk/base collisions for large props.

2. Material/detail pass
- Apply tileable terrain materials by zone.
- Add sprite overlay cards (grass tufts, moss, edge breakup, debris).
- Use depth layering and render priority for clean overlaps.

3. Composition pass
- Add foreground occluders and background cards.
- Add lights/fog shafts and weather tuning.
- Balance readability with cinematic look.

## 3D Scenery Modeling Workflow (Buildable Now)

Use a **modular kit** approach so designers can assemble maps without custom modeling each time.

### 1) Blockout kit (must-have first)
- Flat terrain shelf tile (`8m x 8m`, `4m x 4m` variants).
- Cliff wall modules (`4m`, `8m`, corner in/out).
- Ramp/stair transition modules.
- Bridge deck modules (straight, short curve, end-cap).
- River bank edge modules (smooth edge + rocky edge).

Build every map with these primitives first, no texture polish required.

Current starter kit scenes:
- `assets/scenes/exploration/modules/terrain/TerrainPlateauModule.tscn`
- `assets/scenes/exploration/modules/terrain/TerrainCliffFaceModule.tscn`
- `assets/scenes/exploration/modules/terrain/TerrainRampModule.tscn`
- `assets/scenes/exploration/modules/terrain/TerrainStairModule.tscn`
- `assets/scenes/exploration/modules/terrain/BridgeSegmentModule.tscn`
- `assets/scenes/exploration/modules/terrain/RiverBankEdgeModule.tscn`
- assembly sandbox:
  - `assets/scenes/exploration/modules/sandboxes/TerrainModuleKitSandboxScene.tscn`

### 2) Detail overlay kit (second pass)
- Ground decals/cards (grass clumps, dirt streaks, pebble patches).
- Cliff lip overlays (moss, roots, edge breakup).
- Foreground occluder cards (branches, leaves, fog cards).
- Background cards (distant trees/mountains).

These are mostly `Sprite3D` cards layered over simple 3D shapes.

### 3) Collision authoring rules
- Collision belongs to physical traversal volumes, not to decorative sprite silhouettes.
- Large props (trees/buildings): collider only around trunk/base.
- Terrain collision as simple primitives first (`BoxShape3D`) before mesh collisions.
- Keep collision nodes grouped under `Collision` in each map.

### 4) Scale and pivots
- Keep one world scale standard:
  - human sprite height should read consistently across all scenes.
- Pivot terrain modules at center-bottom when possible.
- Pivot vertical sprite props at “foot” level (or use your existing foot alignment helpers).
- Avoid arbitrary per-prop scale edits; create correct-sized source modules.

### 5) Material strategy
- Base geometry: tileable materials (terrain, cliff, path, rock).
- Detail look: sprite overlays + normals/emissive where needed.
- Water: dedicated shader/material set, never share with terrain materials.

### 6) Authoring order for each map
1. Layout pathing and traversal goals.
2. Place terrain shelves/cliffs/bridges.
3. Add river channels and water.
4. Add collisions and layer-switch volumes.
5. Add gameplay nodes (NPC/chest/zones).
6. Add set dressing and lighting.
7. Add weather/time-of-day tuning.

This keeps scenery buildable from day one and avoids late-stage rework.

## Scene Variant Rule (When to Make a New Scene)

Not every variation needs its own scene.

Create a separate module scene when the variation is:
- topologically different (e.g., straight cliff vs inner/outer corner),
- collision-different (different blocker shape),
- or frequently reused as a named prefab.

Use one scene with tunable properties when the variation is:
- pure size/range tuning (length/height/width),
- material swap only,
- or small visual offsets.

Practical default:
- start with separate scenes for high-frequency layout pieces (corners, short/long bridge, small/large plateau),
- move to parameterized generator scripts once patterns stabilize.

## River Workflow (Curves, Depth, Obstacles)

Use `RiverPathChannel` for all non-trivial rivers:
1. Draw river path with curve points.
2. Pick style: `SmoothBanks` or `RockyGorge`.
3. Tune bed depth, widths, and materials.
4. Generate obstacle anchors and place rocks/logs manually.
5. Use generated boat path for future boat actors.

For advanced rivers:
- keep river water, bed, and walls in separate materials,
- add branch channels as additional `RiverPathChannel` nodes,
- add local foam/splash planes at turbulence points.

## Sprite + 3D Integration Rules

1. Characters/NPCs remain sprite-based (`Sprite3D`/`AnimatedSprite3D`).
2. Large vegetation/props use sprite-first assets with optional normal maps.
3. Collisions represent physical bases only, not full sprite silhouette.
4. Use layered prop groups (foreground/mid/background) for parallax depth.
5. Keep a consistent pixel scale baseline for all sprite assets.

## Camera and Lighting Targets (HD-2D Look)

1. Fixed cinematic camera angle with constrained pan/zoom.
2. Strong depth separation:
- mild DOF,
- controlled bloom,
- fog cards/volumetrics at depth transitions.
3. Night/rain:
- near-zero ambient fill,
- point/emissive props carry local readability,
- weather tint/fog handled by scene visual systems (not ad-hoc per scene).

## Should You Start Over?

Yes, start a **new V2 scene** for this look.

But do it as a phased rebuild:
- keep old vertical slice runnable,
- use it as behavior regression reference,
- migrate subsystem by subsystem.

This gives cleaner architecture and faster content iteration than patching the old scene structure indefinitely.

## Definition of Done for V2 Proof

1. Multi-level terrain with at least one overpass/underpass path.
2. Curved river with visible depth and separate bed/water/bank materials.
3. Sprite props with proper base-only collisions.
4. One NPC, one chest, one conditional interaction zone.
5. Weather + time-of-day + ambient props all active without visual conflicts.
6. Performance pass complete on target hardware.
