# Arcane Pinball Visual Slice Design

## Goal

Upgrade `Assets/01. Scenes/02. Game.unity` toward a low-detail dark pixel-fantasy look whose visual budget is concentrated on moving pinballs and collisions, while remaining suitable for a PC WebGL build.

## Visual hierarchy

- Preserve the current pixel sprites and gameplay layout.
- Darken and cool the map background so characters and pinballs remain readable.
- Keep characters mostly non-emissive.
- Give active pinballs a compact cyan-violet HDR core and a speed-responsive trail.
- Show collisions with short additive arcane particles rather than full-screen distortion.
- Use restrained bloom, cool color grading, and a soft vignette. UI and ordinary white text must not become primary bloom sources.

## Architecture

`ArcaneGameLook` installs the scene look at runtime only when the active scene is named `02. Game`. It enables URP post-processing on the main camera, builds a runtime global Volume profile, and applies conservative tinting to the named map background. This avoids fragile hand-editing of the large scene YAML.

`PinballArcaneVfx` is attached lazily by `Pinball`, so every pooled or cloned ball receives identical VFX without prefab YAML changes. It owns a TrailRenderer, a duplicate glow sprite, and a small collision ParticleSystem. Both effects use simple single-pass additive shaders loaded from `Resources`, keeping Opaque Texture and Depth Texture disabled.

## Performance constraints

- Unity `6000.0.79f1`, URP `17.3.0`, 2D Renderer.
- PC browser target at 1920x1080 and 60 fps.
- No custom full-screen renderer feature, refraction, blur, depth texture, or opaque texture.
- One trail and one preallocated collision particle system per pooled pinball.
- No per-frame material allocation and no unbounded particle spawning.
- Bloom high-quality filtering remains disabled.

## Verification

Static validation checks that all required assets and Unity meta files exist, shaders expose a `Universal2D` pass, the game camera has HDR and post-processing enabled by runtime code, and Pinball forwards activation, deactivation, velocity, and collision events to the VFX component. Final visual and WebGL build verification must be performed in Unity `6000.0.79f1`, which is unavailable in the current execution environment.
