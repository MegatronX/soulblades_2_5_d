# Status Effects Expansion (Alpha)

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
