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
- Keep turn-preview simulation extensible via interfaces (preview stat-delta providers) instead of concrete status/effect type checks inside `TurnManager`.
- In animated UI preview pipelines, never drop update requests while animating; coalesce and apply the latest pending state after tween completion.
- For action impact VFX, use an explicit completion contract (e.g., OneShot task/signal) and await it in battle flow; fixed timers are insufficient for varied animation lengths.

## 2026-02-24
- Keep `BaseCharacter` focused on core character concerns; route status/stat-driven presentation (tint, idle swaps, animation speed feedback) through a dedicated visual-state controller node.
- For status-driven animation speed feedback, anchor scaling to a stable reference speed (base stat or explicit override) and include a runtime poll fallback so late-applied effects are always reflected.
- When speed-changing statuses are applied during an action, defer animation speed-scale updates until the actor's action reaction phase completes to avoid mid-sequence visual pops.
- For action-time speed feedback deferral, lock visual updates for all active combatants during action processing, not only the initiator, since targets can receive speed-changing statuses mid-sequence.

## 2026-02-25
- For presentation sandboxes, always include an explicit “focus view” mode that hides control panels so character sprite/state changes can be inspected unobstructed.
- In visual-debug sandboxes, pair stat/status readouts with a live scene-component tree so sprite/animation node state is observable while testing.
- In mixed 2D+3D debug scenes, avoid fully opaque fullscreen `ColorRect` overlays when expecting 3D visibility; focus mode should explicitly hide/dim backdrop layers.
- For secondary preview windows in Godot, use a `Window` + `SubViewport` and bind `SubViewport.World3D` to the main viewport world so both windows render the same live character instance.
- Avoid opening embedded (`GuiEmbedSubwindows=true`) `Window` previews over active tool UIs; they can intercept clicks and make controls feel non-interactive. Gate detached preview features behind `embed_subwindows=false`.
- For focus-mode character sandboxes, explicitly make the sandbox camera current and re-frame on state changes (focus toggle/respawn); relying on static scene camera setup can leave users with an empty/gray view.
- For visual inspection workflows, add direct mouse-wheel camera zoom in focus mode to reduce iteration friction when tuning sprite/status presentation.
- In sandboxes with sprite-heavy visuals, avoid continuous low-lerp camera drift by default; it can look like ghosting/after-images. Prefer immediate camera placement unless smoothing is explicitly needed.
- For mouse-wheel tooling inputs, prefer `_Input` over `_UnhandledInput` when reliability matters, because intermediate controls can consume wheel events before unhandled phase.
- For timed-window UX feedback, consume the detailed timing signal (signed offset) and map to human-readable early/late bands; this preserves core gameplay ratings while explaining misses clearly to players.
- For Sprite3D pixel-art sheets, avoid mipmaps + VRAM compression and use nearest filtering; compressed+mipmapped imports commonly produce subtle frame shimmer/drift even when source frames are aligned.
- Reuse existing status visual metadata before adding bespoke logic; if a generic field like `ScaleMultiplier` exists, hook it into visual state controller once and drive per-status behavior from resources.
- When adding new status visuals (e.g., mirror after-images), keep them in `CharacterVisualStateController` with data-driven status fields so effect resources control tuning without custom per-character code.
