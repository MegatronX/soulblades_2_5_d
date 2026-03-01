# River Flow Map Authoring Guide

This guide covers how to author curved/bending river flow maps for:

- Shader: `res://assets/shaders/exploration/river_flow.gdshader`
- Material: `res://assets/resources/exploration/materials/forest_river_flow_material.tres`

## 1) Enable Flow Map In Material

In the river material inspector:

1. Set `use_flow_map = true`
2. Assign a texture to `flow_map`
3. Set `flow_map_influence`:
- `0.0` = ignore map, only `flow_direction`
- `1.0` = fully use map vectors

## 2) Channel Convention

Flow vectors are encoded in `R/G` and remapped from `0..1` to `-1..1`.

- `R` controls horizontal flow (U/X)
- `G` controls vertical flow (V/Y)
- `B/A` are currently unused

Mapping reference:

- `R = 0.0` -> full left
- `R = 0.5` -> neutral horizontal
- `R = 1.0` -> full right

- `G = 0.0` -> full up (toward lower UV V)
- `G = 0.5` -> neutral vertical
- `G = 1.0` -> full down (toward higher UV V)

## 3) Authoring Workflow (Curves/Bends)

1. Create a texture matching your river UV layout.
2. Paint direction vectors along the river path:
- Straight segment: constant color vector.
- Bend: smoothly rotate vector colors through the bend.
- Split/merge areas: blend vectors gradually; avoid hard seams.
3. Save as PNG and assign to `flow_map`.
4. Tune `flow_map_influence` (start around `0.7 - 1.0`) and `flow_speed`.

## 4) Style Guidance For HD-2D

- Keep vector transitions smooth and broad (avoid noisy per-pixel vectors).
- Let shader detail/noise provide micro-motion; flow map should define macro direction.
- Use subtle motion speeds (`0.12 - 0.35`) for believable stylized water.

## 5) Import Tips

For flow-map textures, prefer data-accurate import:

- Avoid strong lossy compression.
- Keep filtering consistent with your look:
- `Linear` for smoother direction blending.
- `Nearest` if you intentionally want crisp vector steps.

## 6) Quick Debug Checks

If bends look wrong:

- Verify `use_flow_map` is enabled.
- Raise `flow_map_influence` toward `1.0`.
- Ensure UVs on the river mesh are not mirrored in unexpected sections.
- Check for abrupt color jumps in `R/G` around bend transitions.
