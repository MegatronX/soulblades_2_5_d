# Exploration Map Authoring Workflow

This project now includes a reusable exploration stack for HD-2D style maps.

## Core Scene Pattern

1. Start from `assets/scenes/exploration/templates/ExplorationMapTemplate.tscn`.
2. Set unique `MapId` on the root `ExplorationMapController`.
3. Build world geometry under `EnvironmentGeometry`.
4. Place interaction content under `Interactables`.
5. Place layer transitions under `LayerSwitchVolumes`.
6. Place map entry points under `SpawnPoints` using `MapSpawnPoint`.

## Layering Model (Upper/Lower/Bridge)

- Use `MapLayerSwitchVolume` at stairs/ramps/underpasses.
- Player receives/uses `MapLayerParticipant` automatically.
- Each layer switch updates sprite render priority offset so actor draws correctly as they move between layers.
- Physical over/under behavior should still be authored with 3D collision volumes (bridge colliders above lower paths).

## Interactions

All interactables implement `IExplorationInteractable`:

- `NpcInteractable`:
  - action-triggered.
  - use condition/effect resources for later dialogue/cutscene integration.
- `TreasureChestInteractable`:
  - one-time open via `OneShot`.
  - persistent key via `PersistentStateKey`.
  - open/closed visual textures + reward item/quantity.
- `InteractionZone`:
  - `TriggerMode = StepEnter` for auto triggers.
  - `TriggerMode = Action` for optional player-initiated triggers inside the area.

Condition resources:
- `PartyMemberPresentCondition`
- `MapFlagCondition`

Effect resources:
- `ForceEncounterInteractionEffect`
- `TransitionToMapInteractionEffect`
- `SetMapFlagInteractionEffect`
- `GrantItemInteractionEffect`
- `LogInteractionEffect` (debug/prototyping)

## Map State Persistence

- Runtime persistence is keyed by `MapId` + object key in `MapRuntimeStateStore`.
- One-shot objects should always set explicit `PersistentStateKey`.
- Returning from battle reuses `IBattleReturnStateProvider` in `ExplorationMapController` to restore actor position/layer.

## Atmosphere/Weather/Ambience

- `SceneAtmosphereSystem`:
  - assign `InitialProfile`.
  - set `ApplyInitialProfileOnReady = true` for static biome feel.
- `WeatherSystem`:
  - assign `InitialWeather`.
  - set `ApplyInitialWeatherOnReady` depending on map.
  - ambient loop behavior is controlled by weather profile (`ForceAmbientLoop`).
- `AmbientPropSystem`:
  - assign `AmbientPropProfile`.
  - includes firefly/owl proof-of-concept profile:
    `assets/resources/exploration/ambient/forest_ambient_props.tres`

## Map Music

- Use `ExplorationMusicController` on the map root.
- Assign `MusicLibrary` with weighted `BattleMusicData` tracks.
- Enable `PlayRandomOnReady` for random variety on map load.
- Use interaction effects for dynamic music behavior:
  - `SetExplorationMusicInteractionEffect` to switch tracks (specific or random).
  - `AdjustExplorationMusicMixInteractionEffect` to change runtime volume/pitch mood.

## Vertical Slice Scene

- Proof-of-concept scene:
  `assets/scenes/exploration/ForestExplorationVerticalSlice.tscn`
- Includes:
  - upper/lower traversal + bridge
  - NPC interaction
  - one-time chest reward
  - step-trigger forced battle
  - action-trigger map transition
  - atmosphere + weather + ambient wildlife props
