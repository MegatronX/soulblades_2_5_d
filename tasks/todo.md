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
