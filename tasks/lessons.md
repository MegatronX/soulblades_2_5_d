# Lessons

## 2026-02-23
- For non-trivial combat architecture work, create and maintain `tasks/todo.md` with explicit verification gates before implementation and completion.
- When adding new status-effect subclasses, first confirm the exact virtual hook surface in `StatusEffect` and align overrides before broad implementation.
- For large debug/sandbox UIs, keep node naming deterministic and path-driven so controller wiring stays stable and build-time checks catch errors early.
- When DOT tuning is requested, expose data-level tick mode options (`flat`, `percent`, `max(flat, percent)`) in shared status bases before adding per-status bespoke logic.
- For resource remap abilities (HP/MP inversion), remap both max-stat lookups and current pools on binding changes so effective resource values match player expectations.
- If action comparison logic is needed by status rules, place it in shared utilities (`StatusRuleUtils`) rather than private per-status helpers.
- For debug overlays, expose draw-layer and font-size as tunables and default layer high enough to stay above gameplay UI.
- In `TurnManager`, never persist `SimulatedStats` on live combatants; keep simulation snapshots ephemeral so runtime speed/status changes immediately affect turn order.
- Turn-order previews must include deterministic action outcomes (tick-cost adjustments and guaranteed status applies); otherwise preview ordering diverges from committed turns.
