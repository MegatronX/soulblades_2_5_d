# Status Effects Expansion (Alpha)

---

# HD-2D Vertical Slice V2

## Plan
- [x] Write a concrete HD-2D workflow and migration strategy doc.
- [x] Create `ForestExplorationVerticalSliceV2.tscn` from template with V2 layer stack (`Geometry`, `Collision`, `Gameplay`, `SetDress`) while reusing template-provided system nodes.
- [ ] Port exploration systems to V2 (`SceneVisualDirector`, atmosphere, weather, ambient props, music, encounters).
- [x] Build graybox terraces + one overpass/underpass traversal path.
- [x] Integrate `RiverPathChannel` as curved river authoring baseline in V2.
- [ ] Add initial gameplay interactions (NPC, chest, conditional interaction zone).
- [ ] Runtime verify camera readability + traversal + layer switching.
- [ ] Build verification.

## Review
- [x] Added workflow doc: `assets/resources/exploration/hd2d_vertical_slice_v2_workflow.md`.
- [x] Added `assets/scenes/exploration/ForestExplorationVerticalSliceV2.tscn` scaffold with geometry/collision/river/layer-switch layout.
- [ ] Runtime verify V2 scene once scaffolded.

---

# Terrain Module Kit Scaffold

## Plan
- [x] Create reusable terrain blockout module scenes as separate pieces (plateau, cliff, ramp, stairs, bridge, bank edge).
- [x] Add per-module collision and snap-point markers for stitching in map scenes.
- [x] Add a module assembly sandbox scene to preview module stitching quickly.
- [x] Add quick module usage documentation.
- [ ] Runtime verify module scene parsing and assembly in editor.

## Review
- [x] Added module kit under `assets/scenes/exploration/modules/terrain/`.
- [x] Added sandbox scene: `assets/scenes/exploration/modules/sandboxes/TerrainModuleKitSandboxScene.tscn`.
- [x] Added docs: `assets/scenes/exploration/modules/terrain/README.md`.

---

# Terrain Variant Pack (Corners/Lengths/Curves)

## Plan
- [x] Add small plateau variant (`4m x 4m`) for tighter traversal spaces.
- [x] Add cliff corner variants (inner + outer) for layer turns and canyon bends.
- [x] Add bridge length variants (short + long) for quick composition without scaling base modules.
- [x] Add curved river bank module for non-linear shoreline blockout.
- [x] Update module sandbox to include new variants for immediate stitch testing.
- [ ] Runtime verify variant modules align cleanly with existing `SnapPoints` workflow.

## Review
- [x] Added new variant scenes in `assets/scenes/exploration/modules/terrain/`.
- [x] Updated sandbox: `assets/scenes/exploration/modules/sandboxes/TerrainModuleKitSandboxScene.tscn`.
- [x] Updated module docs: `assets/scenes/exploration/modules/terrain/README.md`.

---

# River Path Authoring Pass

## Plan
- [x] Fix `RiverSandboxScene` visibility issue so 3D river content is actually visible at runtime.
- [x] Add a path-authored river generator (`RiverPathChannel`) that supports dragging curve points in editor.
- [x] Support adjustable cross-section for both `SmoothBanks` and `RockyGorge` styles on path rivers.
- [x] Add generated boat support path and optional moving boat preview.
- [x] Add generated obstacle anchors along river path for placing jutting river obstacles.
- [x] Add dedicated authoring sandbox scene for path river workflows.
- [x] Build verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes.
- [ ] Runtime verify:
  - [ ] `RiverSandboxScene.tscn` shows river visuals immediately.
  - [ ] `RiverPathAuthoringSandboxScene.tscn` allows drag-editing path points and live geometry rebuild.
  - [ ] obstacle anchors and boat path update when curve/params change.

---

# River Sandbox Tuning Scene

## Plan
- [x] Add a standalone exploration river sandbox scene for isolated river iteration.
- [x] Add a dedicated sandbox controller with runtime controls for `RiverChannel` geometry and water shader parameters.
- [x] Add quality-of-life controls: hide/show tuning UI, preset buttons, reset to defaults, mouse-wheel camera zoom, copy-to-clipboard snippet export.
- [x] Expose `RiverChannel.RefreshGeometry()` so runtime tuning changes rebuild channel meshes immediately.
- [x] Build verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes.
- [ ] Runtime verify in `RiverSandboxScene.tscn`:
  - [ ] sliders update river geometry live.
  - [ ] shader controls update water flow/foam live.
  - [ ] copied snippet pastes cleanly into scene/material tuning workflow.

---

# River Channel Geometry Pass

## Plan
- [x] Add a reusable exploration `RiverChannel` scene + script with toggles for `SmoothBanks` and `RockyGorge`.
- [x] Support independent river materials (terrain/banks, bed, water, gorge wall) so rivers can use textures separate from base forest floor.
- [x] Integrate `RiverChannel` into `ForestExplorationVerticalSlice.tscn` in gorge mode.
- [x] Split forest ground visuals around the river corridor so channel depth renders instead of flat overlap.
- [x] Add a short authoring guide for river channel setup and depth/collision expectations.
- [x] Build verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes.
- [ ] Runtime verify in `ForestExplorationVerticalSlice.tscn`:
  - [ ] `RockyGorge` renders with visible depth and separate bank/bed/water materials.
  - [ ] switching to `SmoothBanks` yields a mostly level river profile without scene edits.
  - [ ] no unwanted z-fighting with ground visuals near the channel.

---

# Ambient Spawn Model Simplification

## Plan
- [x] Remove legacy `PropScenes` from `AmbientPropProfile`.
- [x] Remove `PropScenes` fallback path from `AmbientPropSystem` so `SpawnEntries` is the only spawn source.
- [x] Update ambient profile resources to stop serializing `PropScenes`.
- [ ] Build verification.

## Review
- [ ] Runtime verify ambient spawning behavior unchanged with `SpawnEntries`-only model.

---

# Firefly Visibility and Ambient Motion

## Plan
- [x] Stabilize `FireflyAmbientProp` defaults for single-frame/custom sprite usage (prevent frame-grid disappearance).
- [x] Slow firefly glow pulse defaults to avoid rapid glow/un-glow flicker.
- [x] Add optional system-driven ambient prop motion and lifetime settings to `AmbientPropSpawnEntry`.
- [x] Extend `AmbientPropSystem` runtime to apply entry-driven motion and lifetime despawn behavior.
- [x] Add explicit animated-frame count limiting for firefly sprite sheets so few-frame animated textures do not step into empty frames.
- [x] Add explicit animated-frame start index support so firefly animation can target a contiguous valid frame subset (e.g., lower sprite-sheet row only).
- [x] Restore slow default frame animation on `FireflyAmbientProp` while keeping single-frame fallback support in script.
- [x] Add per-entry spawn cooldown support so each prop entry can have independent spawn pacing.
- [x] Add profile option to constrain ambient prop spawn positions to the active camera view.
- [ ] Runtime verify in exploration scene:
  - [ ] firefly sprite remains visible with custom single-frame sprite.
  - [ ] firefly animated few-frame sprite plays without disappearing.
  - [ ] per-entry cooldown produces expected independent spawn pacing.
  - [ ] spawn-only-in-view keeps ambient props within camera framing.
  - [ ] entry-configured motion/lifetime props drift and despawn as expected.

## Review
- [ ] Build verification.

---

# Weather Utils Extraction

## Plan
- [x] Extract weather-local combatant validation helper into shared gameplay utility.
- [x] Extract weather-local list shuffle helper into shared RNG utility.
- [x] Update `WeatherSystem` to consume shared utility methods.
- [ ] Verify where else these helpers should be adopted incrementally (AI/targeting systems).

## Review
- [ ] Build verification.

---

# Scene Visual Director Refactor (Phase 1)

## Plan
- [x] Add shared scene-visual primitives (`SceneVisualIntent`, contribution layers) and a central `SceneVisualDirector`.
- [x] Integrate `SceneAtmosphereSystem` with director contributions for ambient/fog/main-light while keeping sun-shaft/diffuse-fill layers local.
- [x] Integrate `WeatherSystem` with director contributions for weather + time-of-day lighting while keeping particles/audio/lightning local.
- [x] Keep fallback behavior when no director exists to avoid breaking legacy scenes.
- [x] Wire exploration map template to include `SceneVisualDirector` and connect atmosphere/weather nodes.
- [x] Build verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes.
- [ ] Runtime verify in `ForestExplorationVerticalSlice.tscn`:
  - [ ] weather + atmosphere stack predictably (no subsystem tug-of-war).
  - [ ] time-of-day updates remain stable with/without auto-advance.
  - [ ] lightning flashes restore correctly with director orchestration.

---

# Forest Night Lighting Tuning

## Plan
- [x] Identify night-time brightness sources in the vertical slice (weather ambient multiplier + atmosphere fill lights).
- [x] Apply darker night defaults in `ForestExplorationVerticalSlice.tscn` for weather and atmosphere runtime multipliers.
- [x] Verify project compiles after scene updates.
- [x] Make weather overcast ambient boost day-weighted so it does not unintentionally brighten nighttime scenes.
- [x] Push vertical slice to deep-night rain defaults (very low ambient, no moon key light, minimal atmosphere fill).
- [x] Add weather time-of-day post-adjustment controls (brightness/saturation) to darken final scene output at night.
- [x] Tune forest vertical slice to use deeper night post-adjustment multipliers.
- [x] Retune forest to lighting-only night mode (near-zero ambient, no global post-darkening) so point/prop lights remain full brightness.
- [x] Add night-specific ambient/fog multipliers in `WeatherSystem` to reduce haze/lift without dimming local point lights.
- [x] Tune forest vertical slice to deeper ambient/fog-night values while keeping post-darkening disabled.

## Review
- [ ] Validate deep-night readability and rain mood in `ForestExplorationVerticalSlice.tscn` during runtime playtest.

---

# Firefly Visual Polish

## Plan
- [x] Inspect firefly scene/script to identify white-quad render issue and pulse timing behavior.
- [x] Remove incorrect sprite material override causing box rendering and keep sprite texture-driven visuals.
- [x] Add slower configurable glow/alpha pulse controls for fireflies in `FireflyDriftProp`.
- [x] Add optional slow frame animation controls for sprite-sheet firefly twinkle.
- [x] Fix atlas-frame handling when frame animation is disabled so sprite sheets do not pop/disappear from forced `hframes/vframes` reset.
- [x] Fix ambient spawn order so spawned props receive final position before `_Ready` origin capture (prevents first-frame snap/disappear).
- [ ] Validate firefly look in runtime scene.

---

# Ambient Prop Polish Pass

## Plan
- [x] Add spawn/despawn fade support in `AmbientPropSystem` to smooth visual pop-in/out.
- [x] Add optional ambient debug overlay (active count + spawn/despawn reason tracking) in `AmbientPropSystem`.
- [x] Extract firefly motion/glow tuning into reusable `FireflyVisualProfile` resource and wire `FireflyDriftProp` to consume it.
- [x] Update `FireflyAmbientProp.tscn` to use the new profile for tuning.
- [x] Build verification.
- [x] Add recent despawn event logging (timestamp + reason + prop label) to ambient debug overlay.

## Review
- [ ] Runtime verify in `ForestExplorationVerticalSlice.tscn`:
  - [ ] Fireflies fade in/out smoothly (no hard pop).
  - [ ] Ambient debug overlay toggles and reports useful spawn/despawn reasons.
  - [ ] Ambient debug overlay shows rolling recent despawn events in real-time.
  - [ ] Firefly behavior remains tunable via profile resource without recompiling.

---

# Editor Export Property Clarity

## Plan
- [x] Add clearer inspector group labels for ambient/firefly exported properties.
- [x] Add unit-aware export hints (`suffix:s`, `suffix:m`, etc.) where appropriate.
- [x] Keep property names/data model stable to avoid scene/resource migration risk.
- [x] Build verification.

## Review
- [ ] Inspector authoring feels clearer for `AmbientPropProfile`, `AmbientPropSpawnEntry`, `AmbientPropSystem`, and `FireflyVisualProfile`.

---

# Tree Prop Normal Mapping Support

## Plan
- [x] Add a reusable Sprite3D lighting helper script that supports optional normal map assignment.
- [x] Wire tree prop scenes to use the helper script so normal maps can be assigned in-editor.
- [x] Keep default visuals unchanged when no normal map is assigned.
- [x] Build verification.
- [x] Generalize the helper for multi-sprite targets and reuse it for mushroom/crystal prop scenes.

## Review
- [ ] Tree props accept normal map textures in inspector and still render correctly without them.
- [ ] Mushroom/crystal props accept normal map textures via shared helper without affecting glow overlay sprites.

---

# Forest River Flow Pass

## Plan
- [x] Add a stylized HD-2D river surface shader with UV flow animation, highlights, and edge foam.
- [x] Add optional flow-map input to support directional bends/curves in river flow.
- [x] Create a reusable river material resource with tuned defaults for the forest scene.
- [x] Wire `EnvironmentGeometry/River` in `ForestExplorationVerticalSlice.tscn` to use the river flow material.
- [x] Add a flow-map authoring guide resource with channel conventions and bend/curve workflow.
- [x] Build verification.

## Review
- [ ] River surface visibly flows in the forest scene and retains stylized HD-2D readability.
- [ ] Curved-river support is available through flow-map authoring on the material.

---

# Float Hover Visual

## Plan
- [x] Add a reusable resource-driven hover visual effect type for persistent status/ability visuals.
- [x] Extend persistent visual state accumulator/controller to support hover lift+bob offsets.
- [x] Attach hover visual effect to `Float` status resource.
- [x] Add selectable hover bob waveform in resource data (sine/smooth/triangle/sawtooth).
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.

---

# Mirror Images Overlay Alignment Fix

## Plan
- [x] Remove camera-forward/depth positioning from shader overlay sync to prevent Y drift on pitched cameras.
- [x] Keep overlay transforms locked to source sprite and apply only stable horizontal pixel offsets per layer.
- [x] Keep expanded overlay render area while avoiding jitter from camera-space offset recomputation.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.

---

# Mirror Images Clipping/Jitter Cleanup

## Plan
- [x] Remove Mirror Images shader visual layer from status resource to avoid frame-bound clipping artifacts.
- [x] Keep Mirror Images on sprite-clone ghosts only.
- [x] Move ghost placement to deterministic pixel offsets (no camera-space sinusoidal wobble).
- [x] Apply clone offsets/tint to all `SpriteBase3D` variants and render ghosts mostly behind source.
- [x] Bias clone placement to trailing direction using `FlipH`/`FlipV` and increase ghost visibility.
- [x] Add slight Y/Z separation and smooth drift motion; wire `Spread`/`DriftSpeed`/`Alpha` to visibly affect ghost presentation.
- [x] Rebalance clone visibility: stronger tint/alpha response and mixed front/behind render priority to avoid over-subtle trails.
- [x] Move mirror trail tuning constants into `MirrorImagesBattleVisualEffect` exports so visual tuning is resource-driven without recompiling.
- [x] Add configurable in/out pulse controls so trailing mirror images can move toward/away from the core character.
- [x] Decouple in/out pulse timing from drift speed so pulse movement always responds to pulse frequency tuning.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.

---

## Plan
- [x] Audit existing statuses/resources and map requested list to existing vs missing.
- [x] Keep existing implementations untouched if already present (`haste`, `stop`, `reflect`).
- [x] Add reusable infrastructure for missing behavior:
  - [x] stack-aware status state + reapply hooks
  - [x] one-shot/consumable status helpers
  - [x] action restriction hooks (menu + server)
  - [x] runtime crit bonus support for per-target effects
  - [x] priority/tick-cost adjustment hook for quickstep
- [x] Implement missing POSITIVE statuses:
  - [x] Protect
  - [x] Shell
  - [x] Regen
  - [x] Barrier
  - [x] Focus
  - [x] Juggernaut
  - [x] Float
- [x] Implement missing NEGATIVE statuses:
  - [x] Poison
  - [x] Burn
  - [x] Shock
  - [x] Bleed
  - [x] Sunder
  - [x] Ruin
  - [x] Slow
  - [x] Silence
  - [x] Confuse
  - [x] Toad
  - [x] Break
- [x] Implement MIXED statuses:
  - [x] Mini
  - [x] Berserk
  - [x] Reflect (charges variant without editing existing reflect)
- [x] Implement ONE-TURN / CONSUMED statuses:
  - [x] Defender
  - [x] Focused
  - [x] Power Strike
  - [x] Ward
  - [x] Echo Cast
  - [x] Hexed Edge
  - [x] Quickstep
- [x] Add status resources under `assets/resources/status_effects/...` for each newly implemented effect.
- [x] Run compile and smoke verification.

## Review
- [x] Build compiles cleanly.
- [x] No regressions in existing `haste`, `stop`, `reflect`.
- [x] Menu disables blocked actions from status restrictions.
- [x] Server rejects blocked actions from status restrictions.
- [x] New statuses have matching behavior to requested definitions (or documented approximation).

## Notes
- Scope remains alpha-first and data-driven.
- Prefer reusable status scripts and resource tuning over bespoke one-off logic.
- Approximations for current engine:
  - `Focus` currently applies magic damage multiplier only (MP cost multiplier is not wired in this combat loop yet).
  - `Hexed Edge` consumes when initiating the next damaging action, not strictly on landed hit.
  - `Float` exposes a ground-hazard immunity flag but hazard-tick logic must query it explicitly.

---

# Status Sandbox Scene

## Plan
- [x] Implement a dedicated status/ability sandbox controller script.
- [x] Add a new sandbox scene with dynamic UI controls for:
  - [x] character spawn/respawn
  - [x] apply/remove/clear statuses
  - [x] turn start/end/full turn simulation
  - [x] direct HP/MP controls
  - [x] action simulation (physical/magic/heal + menu-style validation)
  - [x] ability equip/unequip/trigger testing
  - [x] charge and overflow controls/events
- [x] Wire status-rule validation and target rewrite in sandbox action flow.
- [x] Add live state panels + event logging for rapid iteration.
- [x] Build and verify compile health.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [x] Sandbox scene file added and wired to controller.
- [x] Existing battle scenes/status resources not overwritten by sandbox setup.

---

# DOT Percent Tick Addendum

## Plan
- [x] Add optional percent tick mode to `FlatDotStackingStatusEffect` (`flat`, `percent`, `max(flat, percent)`).
- [x] Apply Bleed tuning to use `max(flat, percent)` so it remains relevant on high-HP targets.
- [x] Add the same optional percent tick mode to `BurnStatusEffect`.
- [x] Run compile verification.

## Review
- [x] Confirm build passes.
- [x] Confirm Bleed uses `max(flat, percent)` via resource settings.

---

# Ability Stat Remap System

## Plan
- [x] Add runtime stat-remap support in `StatsComponent` with winner rules:
  - [x] highest `remapCount` wins
  - [x] tie goes to latest applied
- [x] Add ability `EffectLogic` for applying/removing stat remaps.
- [x] Add reusable stat-remap data resource type for authoring abilities.
- [x] Add sample abilities for HP<->MP swap and Magic->Strength remap.
- [x] Run compile verification.

## Review
- [x] Confirm build passes with new remap scripts.
- [x] Confirm sample abilities are loadable as `Ability` resources.

## Follow-up
- [x] Remap current HP/MP pools when remap bindings change (not only max stat lookup), to support true inversion behavior.
- [x] Verify runtime behavior for `HP/MP Inversion` (6000/900 -> 900/6000).

---

# Berserk Auto-Attack Variant

## Plan
- [x] Add a dedicated AI strategy that always chooses basic attack on valid living targets.
- [x] Add `Berserk` variant status that applies/removes AI override while active.
- [x] Keep status rule enforcement so only basic attack is allowed.
- [x] Add status resource for sandbox/content use.
- [x] Run compile + runtime smoke verification.

## Review
- [x] Build passes.
- [x] Applying/removing the status creates/restores/removes AI override as expected.

## Follow-up
- [x] Extract `AreSameAction(ActionData, ActionData)` from `BerserkAutoAttackStatusEffect` into shared utility.
- [x] Verify compile after utility extraction.

## Refactor
- [x] Introduce generic `AIOverrideStatusEffect` lifecycle base for strategy override apply/remove.
- [x] Move Berserk family to inherit from generic AI-override-capable status base.
- [x] Re-implement `BerserkAutoAttackStatusEffect` as a specific override strategy variant.
- [x] Verify compile + runtime behavior for both:
  - [x] actor without pre-existing `AIController`
  - [x] actor with existing `AIController` strategy/suppression

---

# Battle Category Auto-Populate Filters

## Plan
- [x] Add editor-configurable action-category filters to `BattleCategory`.
- [x] Update `ActionManager.PopulateCategory` to use category filters.
- [x] Keep backward-compatible fallback to existing name-based matching when no filters are set.
- [x] Run compile verification.

## Review
- [x] Build passes with updated battle menu category logic.

---

# Non-Damaging Action Damage Skip

## Plan
- [x] Add shared helper to detect when an action should resolve damage (`DamageComponent` present and non-zero power).
- [x] Skip crit/damage calculation and HP application in battle mechanics for non-damaging actions.
- [x] Suppress damage number popup for hit results with zero total damage.
- [x] Mirror the same non-damaging handling in `StatusSandboxController`.
- [x] Run compile verification.

## Review
- [x] Build passes with non-damaging action skip behavior.

---

# Battle Stats Debug Overlay

## Plan
- [x] Locate any pre-existing in-battle debug overlay patterns and reuse conventions.
- [x] Add a toggleable battle stats overlay that shows full per-combatant stat breakdown.
- [x] Wire the overlay into battle startup for debug builds only.
- [x] Run compile verification.

## Review
- [ ] Confirm overlay toggles on/off in battle (manual runtime check).
- [x] Confirm compile passes.

## Follow-up
- [x] Force stats overlay onto a top debug UI layer so it stays visible above battle UI.
- [x] Increase default stats overlay text size for readability.

---

# Turn Order Speed Recalc Fix

## Plan
- [x] Reproduce/trace why runtime speed changes were not affecting turn order.
- [x] Ensure live turn calculations read current `StatsComponent` values instead of stale simulation snapshots.
- [x] Run compile verification and sanity-check expected behavior.

## Review
- [ ] Confirm hasted unit (11 -> 33 speed) now overtakes 15-speed units in turn order.

---

# Action Preview Status/Speed Sync

## Plan
- [x] Wire target-selection previews to include deterministic applied statuses (100% chance entries).
- [x] Include initiation-phase preview modifiers so tick-cost adjustments and extra status payloads are reflected.
- [x] Update `TurnManager` preview simulation to apply/revert stat changes from data-driven status effects.
- [x] Run compile verification and retest preview behavior.

## Review
- [ ] Confirm Haste preview now reorders cards before commit.

---

# Turn Preview Architecture Cleanup

## Plan
- [x] Unify blocked-turn visibility policy between initial, preview, and post-commit turn-order generation.
- [x] Extract preview assembly from `TargetSelectionController` into shared `ActionPreviewBuilder`.
- [x] Reuse runtime action preparation in preview flow via `ActionDirector` (initiation/global modifiers + tick-cost resolution).
- [x] Replace `TurnManager` status concrete-type checks with explicit preview stat-delta contracts.
- [x] Run compile verification.

## Review
- [ ] Confirm Stop/Haste previews remain in sync with committed turn order.

---

# Turn Preview Rapid-Target Coalescing

## Plan
- [x] Fix dropped preview updates when rapid target changes occur during in-flight turn-order animation.
- [x] Coalesce queued preview requests so latest target state is rendered after current tween completes.
- [x] Run compile verification and retest rapid target switching.

## Review
- [ ] Confirm no stale/older preview state when cycling targets quickly.

---

# Action VFX Orientation and Timing

## Plan
- [x] Ensure OneShot VFX sprite-based effects can face the camera reliably.
- [x] Ensure action flow waits for impact/reaction OneShot VFX completion before ending the action.
- [x] Run compile verification and retest with `Haste.tscn` on enemy targets.

## Review
- [ ] Confirm Haste impact sprite appears front-facing to camera.
- [ ] Confirm next turn does not begin before impact/reaction VFX finishes.

---

# Character Visual State Controller

## Plan
- [x] Move status/stat-driven visual responsibilities out of `BaseCharacter` into a dedicated controller node.
- [x] Add status-level visual metadata for persistent tint and injured-idle overrides.
- [x] Add speed-to-animation playback scaling driven by effective `Speed` stat changes.
- [x] Defer speed-feedback playback updates while an action sequence is animating, then apply pending speed after reaction finishes.
- [x] Add idle animation switching (`Idle` <-> `Injured`) based on HP threshold and debilitating status flags.
- [x] Wire battle animation return-to-idle to resolve through the visual controller.
- [x] Add baseline status-resource tuning values for Berserk tint + debilitating injured-idle behavior.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: Haste/Slow/Shock visibly alter animation playback speed.
- [ ] Confirm runtime: Speed feedback no longer pops mid-action; updates apply after reaction phase.
- [ ] Confirm runtime: Berserk applies red tint while active and clears on expiry.
- [ ] Confirm runtime: Poison/Burn/Bleed/Slow/Shock switch idle to `Injured` (if animation exists) and restore when cleared.
- [ ] Confirm runtime: HP < 25% switches to `Injured` and returns above threshold.

---

# Timed Hit Authoring Sandbox

## Plan
- [x] Extend timed-hit runtime model to support richer timing anchors and speed-safe offset interpretation.
- [x] Add timed-hit resolution telemetry (signed early/late error + window index) for tuning feedback.
- [x] Add runtime offset hooks on `ActionContext` for per-action/per-character tuning overrides.
- [x] Build a dedicated timed-hit sandbox scene/controller using real action execution pipeline (`ActionDirector`/`BattleAnimator`/`TimedHitManager`).
- [x] Add editor controls for selecting actor/target/action, triggering action, choosing window index, and adjusting action/character offsets.
- [x] Add optional controls for key action VFX timing knobs (travel delay/duration, impact prewarm) used during tuning runs.
- [x] Add “commit tuned offset” action to write selected window timing back to action resource.
- [x] Run compile verification and document tuning workflow.

## Review
- [ ] Confirm multiple timed windows can be tuned and logged independently.
- [ ] Confirm magic timing can anchor to travel start/end and remains stable under speed changes.
- [ ] Confirm per-character tuning offset applies without mutating base resources until committed.

---

# Character Presentation Sandbox

## Plan
- [x] Add a dedicated single-character presentation sandbox scene/controller.
- [x] Support character respawn from scene library and ensure required components (stats/status/ability/equipment/visual).
- [x] Wire status testing controls (apply/remove/clear + active status list).
- [x] Wire ability testing controls (equip/unequip/trigger + equipped list).
- [x] Wire equipment testing controls (slot selection + equip/unequip + equipped slot list).
- [x] Bind the character to `BattlePartyStatusRow` and keep row updated through runtime changes.
- [x] Add an on-character floating stat panel plus a detailed side stat breakdown panel.
- [x] Add utility controls for HP/MP/charge and turn start/end to inspect presentation changes quickly.
- [x] Compile and verify scene/script wiring.

## Review
- [ ] Confirm status, ability, and equipment operations update visuals + row + stat panels in the sandbox.
- [ ] Confirm floating stat panel tracks the character in-world and remains readable.

---

# Character Preview Window Input Blocking

## Plan
- [x] Reproduce/trace why sandbox UI buttons became non-clickable after preview-window changes.
- [x] Guard preview-window usage so embedded subwindow mode cannot open a blocking window.
- [x] Set project subwindow mode to detached (`embed_subwindows=false`) for true secondary-window behavior.
- [x] Run compile verification.

## Review
- [x] Build passes with no new errors.
- [ ] Confirm runtime: buttons remain clickable in `CharacterPresentationSandboxScene` and detached preview opens as its own OS window after restart.

---

# Character Focus View Visibility + Zoom

## Plan
- [x] Investigate why `Focus Character View` can show a gray/empty view instead of the character.
- [x] Add explicit sandbox focus-camera control (force camera current + immediate framing on focus/respawn).
- [x] Add mouse-wheel zoom in focus mode for quick sprite inspection.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: entering focus view shows the character consistently.
- [ ] Confirm runtime: mouse wheel zooms in/out while in focus view.

---

# Character Focus Camera Ghosting + Zoom Reliability

## Plan
- [x] Investigate focus-view visual ghosting/after-image artifacts.
- [x] Make zoom handling deterministic by consuming wheel input in `_Input` instead of `_UnhandledInput`.
- [x] Reduce camera drift by defaulting focus follow interpolation to immediate placement.
- [x] Restrict focus-camera follow updates to focus mode only.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: focus view no longer shows ghosting/distortion.
- [ ] Confirm runtime: scroll zoom responds consistently.

---

# Timed Window Timing-Band Feedback

## Plan
- [x] Add a shared timing-band mapper for signed timing offsets:
  - [x] `Early`
  - [x] `Great (Early)`
  - [x] `Perfect`
  - [x] `Great (Late)`
  - [x] `Late`
- [x] Hook `TimedHitResolvedDetailed` into battle presentation flow.
- [x] Show faint timing text on timed-window resolution (including misses), with fallback when ring UI is absent.
- [x] Keep behavior scoped to actions that actually open timed windows.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: labels appear for both on-time and missed inputs and feel readable/non-intrusive.

---

# Ceira Sprite Frame Drift Investigation

## Plan
- [x] Audit Ceira sprite scene/import settings for pixel-stability issues.
- [x] Disable mipmaps + VRAM compression on `ceira_new_1.png` import.
- [x] Force nearest filtering on Ceira `Sprite3D`.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: idle loop no longer exhibits subtle per-frame drift/shimmer and reduced 9->0 loop pop.

---

# Mini Status Visual Shrink

## Plan
- [x] Implement status-driven sprite scale modifier in `CharacterVisualStateController`.
- [x] Use existing `StatusEffect.ScaleMultiplier` as the data-driven visual hook.
- [x] Set Mini status resource to apply shrink scaling.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: applying Mini shrinks afflicted sprite; removal restores base size.

---

# Mirror Images Status Effect

## Plan
- [x] Add a new `MirrorImagesStatusEffect` gameplay rule that doubles evasion by reducing incoming action accuracy.
- [x] Extend status visual metadata with mirror-image presentation parameters.
- [x] Implement mirror-image ghost sprite rendering in `CharacterVisualStateController`.
- [x] Add a new `Mirror Images` status resource with tuned defaults.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: applying Mirror Images shows multiple translucent after-image sprites and removal fully cleans up visuals.

---

# Composable Status/Ability Visual Effects Architecture

## Plan
- [x] Add a generic visual effect resource stack (`BattleVisualEffect`) with event triggers + runtime context.
- [x] Add a persistent visual state accumulator and wire it into `CharacterVisualStateController`.
- [x] Route status lifecycle/action hooks to visual effects via `StatusEffectManager` and `BattleMechanics` (not GlobalEventBus).
- [x] Add ability-effect visual effect support for trigger dispatch + persistent contributions.
- [x] Implement reusable effects: tint, scale, mirror images, one-shot event VFX.
- [x] Migrate Mirror Images status resource to the new visual effect list and remove mirror-specific fields from base status.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: Mirror Images still doubles evasion and uses after-image visuals.
- [ ] Confirm runtime: legacy status visuals (tint/scale/shader) still render as before.

---

# Decouple Mechanics Hook Events From Visual Routing

## Plan
- [x] Add generic battle hook event types/payloads for modifier and lifecycle hooks.
- [x] Refactor `BattleMechanics` to emit generic hook events rather than visual dispatch calls.
- [x] Refactor `StatusEffectManager` and `AbilityManager` lifecycle/trigger hooks to emit generic hook events.
- [x] Route visual-effect dispatch in `ActionDirector` by subscribing to hook events.
- [x] Verify compile.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: status/ability one-shot visuals still trigger during real battles.

---

# Mirror Images Visibility Follow-up

## Plan
- [x] Normalize status/ability visual-effect storage to `Array<Resource>` for robust Godot serialization.
- [x] Update visual-effect dispatchers to cast resources to `BattleVisualEffect` before execution.
- [x] Add sandbox logging to show resolved visual-effect types and active mirror ghost count.
- [x] Ensure generated mirror ghost sprites inherit source render layers/visibility flags.
- [x] Add shader fallback visual effect for Mirror Images to guarantee visible presentation.
- [x] Add shader-overlay sprite path so Mirror shader can render beyond base sprite bounds without modifying base sprite render.
- [x] Remap overlay shader UVs via runtime `overlay_scale` so expanded overlay margins actually render trails beyond frame bounds.
- [x] Replace single overlay wobble with dual front/back shader overlays using stable offsets to reduce jitter and edge clipping artifacts.
- [x] Verify compile.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: applying `Mirror Images` logs a non-zero ghost count and visible ghost copies in focus view (shader fallback removed in favor of ghost-only visuals).

---

# Remove Legacy Status Visual Overrides

## Plan
- [x] Migrate `Mini` and `Berserk` status visuals to `BattleVisualEffect` resources.
- [x] Migrate all remaining status resources using legacy visual fields (`ForceInjuredIdleAnimation`) to visual-effect resources.
- [x] Remove legacy status visual fields and legacy fallback contribution path from runtime code.
- [x] Add explicit `BattleVisualEffect` resources for injured-idle and keep shader support via visual-effects.
- [x] Verify compile.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: `Mini` scale, `Berserk` tint, and DOT injured-idle presentation all still render as expected.

---

# Weapon Attack Rating + Formula Pipeline

## Plan
- [x] Add weapon attack ratings to equippable data (physical and magical channels).
- [x] Add pluggable weapon-power formula resources with runtime context (holder + target + action context + battle mechanics/battlefield root).
- [x] Wire weapon rating contribution into `StandardDamageStrategy` damage calculation.
- [x] Add optional equipment-driven `Attack` command override so weapons can replace default basic attack command.
- [x] Verify compile health.

## Review
- [ ] Confirm runtime: fixed-rating weapon increases outgoing attack damage as tuned.
- [ ] Confirm runtime: formula weapon updates power based on live state (e.g., missing HP/status count).
- [ ] Confirm runtime: equipping/unequipping weapon with `AttackCommandOverride` swaps/restores the root `Attack` command.

---

# Battlefield Global Modifiers (Weather/Terrain Hooks)

## Plan
- [x] Add a generic scene-level `BattlefieldEffect` resource type implementing existing action modifier hooks.
- [x] Add `BattlefieldEffectManager` node to host active battlefield effects.
- [x] Integrate battlefield effects into `BattleMechanics` modifier pipeline (broadcast/target/post-execution hooks).
- [x] Add a generic elemental damage multiplier battlefield effect resource script for weather-like logic.
- [x] Add a sample rain effect resource (Water/Lightning up, Fire down).
- [ ] Verify runtime in a battle scene with an active manager/effect.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Confirm runtime: enabling rain effect visibly changes Water/Lightning/Fire action outputs.

---

# Weather System (Scene + Battle)

## Plan
- [x] Add reusable scene-level weather resources (`WeatherProfile`, `WeatherTurnHazard`) and enums.
- [x] Add reusable `WeatherSystem` node for visual/audio ambience and scene condition multipliers.
- [x] Integrate battle-facing weather hooks:
  - [x] inject/remove weather battlefield effects through `BattlefieldEffectManager`
  - [x] periodic turn-based weather hazards (storm strikes, hail/sand chip damage)
- [x] Author weather data/resources for Rain, Storm, Snow, Hail, Sandstorm, and Scorching Heat.
- [x] Add a dedicated `WeatherSandboxScene` to preview weather visuals and probe battle weather behavior.
- [x] Wire `MainBattleScene` with `BattlefieldEffectManager` and `WeatherSystem` nodes.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln` passes.
- [ ] Runtime check: weather particle look and density/wind tuning in `WeatherSandboxScene`.
- [ ] Runtime check: storm random lightning hazard, hail/sand periodic chip damage, and rain/heat elemental battle modifiers.

## UI Follow-up
- [x] Refactor `WeatherSandboxScene` to a minimal overlay HUD so preview world remains dominant.
- [x] Add simple 3D set dressing in sandbox for more realistic precipitation readability.
- [x] Update `WeatherSandboxController` node resolution to support the new compact layout while preserving old path fallback.
- [x] Re-run compile verification.

---

# Scene Atmosphere Layer Refactor

## Plan
- [x] Convert `SceneAtmosphereSystem` from monolithic logic into a thin orchestrator.
- [x] Route apply/update/clear through pluggable `IAtmosphereLayer` implementations using `SceneAtmosphereRuntimeContext`.
- [x] Preserve existing scene-facing API/exports (`SetProfile`, runtime multipliers, scene references) for compatibility.
- [x] Add runtime layer management hooks (`AddLayer`, `RemoveLayer`, `SetLayers`) for future extensibility.
- [x] Run compile verification.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes.

---

# Workflow Compliance Refresh

## Plan
- [x] Re-read and align with the user’s workflow constraints before further implementation work.
- [x] Confirm non-trivial tasks always begin with a written checklist in `tasks/todo.md` (plan + verification gates).
- [x] Confirm user correction handling always updates `tasks/lessons.md`.
- [x] Confirm progress updates include high-level step summaries during execution.
- [x] Confirm completion criteria always include concrete verification evidence before marking done.

## Review
- [x] Compliance refresh recorded and will be applied as default workflow for subsequent tasks in this session.

---

# Explorable Map Framework (Forest Vertical Slice)

## Plan
- [x] Define exploration scene architecture with pluggable subsystems (layering, interaction, transitions, ambience, state persistence).
- [x] Implement base exploration scene template and reusable subsystem nodes/scripts.
- [x] Implement map layering model (upper/lower traversal, bridge/underpass behavior, render-order layer switching volumes).
- [x] Implement generic interaction framework (`IInteractable` + conditions + effects) supporting:
  - [x] NPC interaction stubs
  - [x] one-time treasure chest with prerequisites and persistent open/closed state
  - [x] step/activate interaction areas with condition checks and event dispatch
- [x] Implement map-state persistence model (per-map object states for revisit consistency).
- [x] Integrate atmosphere/weather into exploration map scene and expose tuning references.
- [x] Implement ambient prop system (visual/audio wildlife props with spawn profiles and optional lightweight behaviors).
- [x] Build forest prototype map scene demonstrating all above systems.
- [x] Build a map authoring workflow doc/checklist for Godot editor usage and content production.
- [x] Verification:
  - [x] run build
  - [ ] run in editor and validate layer switching, interactions, persistence, ambience, and atmosphere behavior.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after framework + scene integration.
- [x] Fixed `ForestExplorationVerticalSlice.tscn` resource parse blockers:
  - removed stale ext_resource UIDs for encounter and goblin texture refs
  - converted hand-authored gradient constructor values to explicit floats
- [ ] Manual editor/runtime validation pending for interaction flow, bridge over/under feel, and transition spawn behavior.

---

# Exploration Controls + Audio Follow-up

## Plan
- [x] Fix exploration forward/back movement inversion by adding explicit vertical-axis inversion control in movement pipeline and wiring map defaults.
- [x] Add explicit weather ambient looping support so ambient weather SFX can loop regardless of source stream import loop flags.
- [x] Implement exploration scene music controller with:
  - [x] weighted random selection from a track list
  - [x] per-map initial playback configuration
  - [x] runtime track switching
  - [x] runtime pitch/volume adjustment
- [x] Add interaction effects that can:
  - [x] switch to a specific/random map track
  - [x] adjust map music mix properties (volume/pitch multipliers)
- [x] Wire the forest vertical-slice scene to demonstrate random map music + trigger-driven music change.
- [x] Verification:
  - [x] run build
  - [ ] manual runtime check in editor for movement direction, weather looping, and map music trigger behavior.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after control/audio follow-up changes.
- [ ] Manual editor/runtime validation pending.

---

# Weather Lightning Dramatic Pass

## Plan
- [x] Make lightning flashes screen-filling by adding a camera-relative lightning burst position and configurable flash range.
- [x] Add optional screen overlay flash and scene-light pulse controls to `WeatherProfile` for per-weather tuning.
- [x] Tune storm/rain profile defaults to stronger lightning presentation.
- [x] Verification:
  - [x] run build
  - [ ] manual runtime check in weather sandbox to validate intensity/readability.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after lightning presentation changes.
- [ ] Manual sandbox validation pending.

---

# Exploration Startup Signal Warnings

## Plan
- [x] Fix weather startup warning caused by disconnecting a nonexistent `AudioStreamPlayer.Finished` connection.
- [x] Fix exploration music startup warning caused by the same signal disconnect pattern.
- [x] Replace event `-=` wiring in these paths with guarded `IsConnected/Disconnect/Connect` signal wiring.
- [x] Verification:
  - [x] run build
  - [ ] manual scene run to confirm warnings are gone on `ForestExplorationVerticalSlice` startup.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after signal wiring fix.
- [ ] Manual runtime warning verification pending.

---

# Forest Position Debug Window

## Plan
- [x] Add a lightweight toggleable position debug UI controller for exploration scenes.
- [x] Wire the debug position window into `ForestExplorationVerticalSlice`.
- [x] Show live player `GlobalPosition` (X/Y/Z) and support quick toggle via hotkey.
- [x] Verification:
  - [x] run build
  - [ ] manual scene run to confirm toggle/visibility and live position updates.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after adding debug position window.
- [ ] Manual runtime validation pending.

---

# NPC Grounding Fix

## Plan
- [x] Prevent `GuideNpc`-style sprite clipping by adding reusable NPC sprite grounding logic.
- [x] Auto-align NPC sprite feet to the node origin using sprite frame height + pixel size.
- [x] Optionally snap NPC root to ground on ready via downward raycast.
- [x] Verification:
  - [x] run build
  - [ ] manual scene run to confirm `GuideNpc` no longer clips into the base layer.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after NPC grounding change.
- [ ] Manual visual validation pending.

---

# Vertical Slice Prop Collisions

## Plan
- [x] Verify whether tree props in the forest vertical slice have physics bodies.
- [x] Add trunk-only collision to tree prop scenes so large canopy sprites remain pass-through while bases block movement.
- [x] Keep collisions reusable by updating prop scenes (not per-instance ad hoc blockers).
- [x] Verification:
  - [x] run build
  - [ ] manual scene run to confirm player collides with trunk area but can move around canopy edges.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after prop collision changes.
- [ ] Manual runtime collision feel validation pending.

---

# Guide NPC Collider Wiring

## Plan
- [x] Convert `GuideNpc` collider wiring from orphan `CollisionShape3D` under `Node3D` to valid `StaticBody3D` + `CollisionShape3D`.
- [x] Preserve existing collider local offset/size while making it physically active.
- [x] Verification:
  - [x] run build
  - [ ] manual scene run to confirm NPC blocks at intended base footprint.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after collider-body wiring fix.
- [ ] Manual runtime validation pending.

---

# Forest Immersion Pass

## Plan
- [x] Add explicit firefly frequency controls by extending ambient spawn profile with weighted entries and per-entry active caps.
- [x] Add firefly sprite support (texture-driven sprite visuals, mesh fallback, optional glow light) in `FireflyDriftProp`/`FireflyAmbientProp`.
- [x] Increase forest ambience density via ambient profile tuning for the vertical slice.
- [x] Make map boundaries feel natural by adding visible blocker props/geometry (trees + thickets) instead of invisible walls.
- [x] Add reusable light-emitting props (mushroom/crystal) and place them in the vertical slice.
- [x] Verification:
  - [x] run build
  - [ ] manual scene run for visual/collision tuning.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after immersion pass changes.
- [ ] Manual runtime playtest pending for ambience spawn feel and boundary traversal.

---

# Weather Overcast Diffuse Lighting

## Plan
- [x] Extend `WeatherProfile` with overcast controls for directional flattening, ambient boost, and shadow suppression.
- [x] Apply overcast lighting behavior in `WeatherSystem` for active weather and proper baseline restoration.
- [x] Tune rain/storm profiles to use overcast diffuse settings in exploration vertical slice contexts.
- [x] Verification:
  - [x] run build
  - [ ] manual scene run to validate reduced harsh shadows and diffuse overcast feel during rain/storm.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after overcast lighting changes.
- [ ] Manual visual validation pending.

---

# Sprite-First Glow Props

## Plan
- [x] Convert mushroom/crystal glow props from mesh-based geometry to `Sprite3D`-based composition to match HD-2D aesthetic.
- [x] Keep pulsing `OmniLight3D` behavior through existing `AmbientLightPulseProp`.
- [x] Keep placeholder textures/material look so art can be swapped in directly later.
- [x] Verification:
  - [x] run build
  - [ ] manual scene run to evaluate style/readability with current camera and weather.

## Review
- [x] `dotnet build SoulBlades_2_5_D.sln -v minimal` passes after sprite-first prop conversion.
- [ ] Manual art pass pending once final mushroom/crystal sprites are provided.
