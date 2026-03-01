# HD-2D Exploration Scene Architecture Notes

Target aesthetic: 3D scene composition + 2D sprite actors/props + cinematic post/lighting.

## Recommended scene stack

1. **Terrain Layer**
- large-form walkable geometry
- cliff steps, terraces, overhangs
- collision authored separately from visuals when needed

2. **Water Layer**
- path-authored channels (`RiverPathChannel`)
- water shader materials with flow controls
- dedicated boat/navigation paths

3. **Prop Layer**
- sprite-based vegetation/rocks/structures
- localized colliders (base/trunk only for large sprites)
- optional normal maps for dynamic lighting response

4. **Gameplay Layer**
- NPCs, interactables, encounters, triggers
- map-layer switching and pathing volumes

5. **Atmosphere Layer**
- weather system + time-of-day
- fog, shafts, ambient wildlife, emissive props

## Authoring principles

- Keep systems **modular**:
  - river generation, weather, atmosphere, ambient props, and interactions should be independent subsystems.
- Avoid one giant “all-in-one map mesh”:
  - preserve replaceable pieces (terrain chunks, river channels, prop kits).
- Prefer **data/resources over code edits** for tuning:
  - materials, profiles, and scene presets.
- Support **preview scenes** per subsystem:
  - weather sandbox, river sandbox, character/status sandbox, etc.

## Why this fits Octopath-like maps

- Curved/terraced terrain can be built in 3D.
- Character/prop visuals stay sprite-driven.
- Cinematic depth comes from camera framing, lights, fog, bloom, and layered geometry.
- Reusable subsystems let you scale from small test maps to large handcrafted areas without rewriting core systems.
