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
- When status/ability visual behaviors start expanding, move to composable visual-effect resources with lifecycle/action hook dispatch at existing runtime call sites (`StatusEffectManager`, `BattleMechanics`, `AbilityManager`) instead of bloating base data classes or routing gameplay hooks through the global bus.
- Keep gameplay systems presentation-agnostic: `BattleMechanics`, `StatusEffectManager`, and `AbilityManager` should emit generic hook payloads (`BattleHookEvent`) while `ActionDirector` (or a presentation router) translates them into visual dispatch.
- For Sprite3D mirror/after-image effects, account for source `alpha_cut` and depth behavior; clone sprites should disable alpha cut and use explicit depth/sorting offsets or they can appear invisible despite being spawned.
- When exporting polymorphic visual-effect lists in Godot resources, store them as `Array<Resource>` and cast at dispatch time; typed custom-resource arrays can deserialize inconsistently and silently drop behavior.
- When introducing a composable visual-effect system, remove legacy parallel visual fields quickly after migration; dual-path visual logic causes drift, harder debugging, and inconsistent status presentation.
- For generated Sprite3D visual clones, always inherit `VisualInstance3D.Layers` and critical sprite render flags (`Transparent`, alpha-cut mode, render priority); otherwise clones can exist but be camera-invisible.
- When a persistent visual effect is logically active but visually subtle/unreliable (e.g., after-images), add a shader-based fallback visual layer so players always get clear feedback.
- For Godot 4 `shader_type spatial`, do not use `TEXTURE` directly; declare an explicit sampler uniform (e.g., `texture_albedo`) and bind textures from runtime sprite data when needed.
- For Godot signal wrappers in C#, avoid unconditional `-=` on engine signals (`Window.CloseRequested`, etc.); use `IsConnected` + `Disconnect`/`Connect` with `Callable` to prevent runtime "disconnect nonexistent connection" errors.
- For afterimage shaders, do not keep `ALPHA = base.a`; include displaced-sample alpha in output or the effect collapses into a plain tint with no visible duplicate silhouettes.
- Match spatial `render_mode` tokens to the exact Godot version in use (`depth_prepass_alpha` vs invalid variants); one bad token hard-fails resource loading at scene startup.
- Sprite shader offsets are still clipped by the sprite quad; to let effects extend beyond frame bounds, expand geometry in `vertex()` and remap UVs back to original content space.
- For pixel-art Sprite3D status visuals, prefer real duplicate sprites for “beyond frame” afterimages; shader-quad expansion can introduce perceived scale/jitter artifacts unless the atlas pipeline is carefully controlled.
- A shader on a single `Sprite3D` cannot draw outside that sprite’s mesh bounds; for out-of-bounds afterimages, use separate ghost drawables instead of forcing base-sprite shader expansion.
- To keep base sprites unchanged while allowing out-of-bounds shader trails, render the shader on a synchronized enlarged overlay sprite (separate drawable) and keep base material untouched.
- Treat shader expansion settings as effect budget, not literal world-scale multipliers; damp conversion from expansion params to overlay scale to avoid oversized duplicate silhouettes.
- Enlarging an overlay sprite alone is insufficient; remap shader UVs back into source content space (via `overlay_scale`) so trails can use the added margins instead of scaling the base sprite.
- For mirror/afterimage readability, prefer static multi-layer offsets (front/back overlays) over time-driven UV wobble; animated UV direction often reads as jitter on pixel sprites.
- For billboard Sprite3D overlay effects, avoid camera-space forward/right position offsets; keep overlays transform-locked to source and use sprite pixel offsets to prevent Y drift and perspective jitter.
- For mirrored ghost overlays, avoid `ghost_alpha - base_alpha` style subtraction as the primary alpha path; it can slice silhouettes in half when layers are offset. Prefer full displaced-silhouette alpha with only light center suppression.
- For Mirror Images-style effects, favor deterministic clone sprites with pixel-space offsets over animated camera-space offsets; this avoids clipping artifacts and subpixel jitter in pixel-art Sprite3D workflows.
- When cloning sprite ghosts, apply offset/tint logic at `SpriteBase3D` level (not only `Sprite3D`) so AnimatedSprite3D-based characters still show visible duplicates.
- For trailing afterimages, derive trail direction from sprite mirroring (`FlipH`/`FlipV`) instead of symmetric spread so ghosts consistently appear behind facing direction.
- If designers report spread/drift/alpha tuning has weak impact, verify those fields are actually consumed in runtime math (especially drift speed) and avoid aggressive clamp/multiplier combinations that flatten parameter response.
- Avoid making all afterimage clones render strictly behind source with low-contrast white-pass; this can make effects visually disappear even when counts/alpha are high. Keep at least one near clone in front and use explicit tint contrast.
- When designers are tuning visuals iteratively, move hardcoded coefficients (offsets, alpha/tint curves, layering) into exported resource fields early to avoid compile-time iteration loops.
- For trailing ghost effects, include an explicit distance pulse control (amplitude/frequency/phase/min scale) rather than overloading drift offsets; this gives clear, tunable “in/out from core” motion.
- Keep pulse timing independent from drift timing; coupling pulse phase to drift speed can make in/out movement appear nonresponsive to pulse-specific tuning.
- For vertical status visuals like Float, apply sprite lift via offset (negative Y) in the shared visual controller and restore base offsets when the effect ends; this avoids per-status transform hacks.
- For hover/bob visuals, expose waveform selection in resource data instead of hardcoding `sin`; designers often need different feel curves without recompiling.
- For formula-driven equipment scaling, pass an extensible runtime context object instead of long parameter lists so future formulas can consume new battle data without signature churn.
- When equipment can override commands (e.g., weapon replacing `Attack`), refresh overrides centrally on equip/unequip and clear fallback explicitly to avoid stale menu state.
