# Arcane Pinball Visual Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a WebGL-conscious dark arcane look and speed-responsive pinball feedback to the `02. Game` scene.

**Architecture:** A scene-gated runtime bootstrap configures URP post-processing and palette treatment without editing the large scene YAML. A focused component attached by `Pinball` owns all ball-local renderers and pooled collision particles, backed by two minimal Resources shaders.

**Tech Stack:** Unity 6000.0.79f1, URP 17.3.0 2D Renderer, C#, ShaderLab/HLSL, Python static validation

## Global Constraints

- Target scene is exactly `02. Game`.
- Target platform is PC WebGL at 1920x1080 and 60 fps.
- Preserve gameplay behavior and existing art assets.
- Keep Opaque Texture and Depth Texture disabled.
- Do not add a custom full-screen renderer feature.
- Allocate no materials or particle systems during collision handling.

---

### Task 1: Static visual-contract test

**Files:**
- Create: `Tools/validate_arcane_vfx.py`

**Interfaces:**
- Consumes: Unity project files under `Assets`.
- Produces: exit code `0` only when the visual-slice assets and source hooks are complete.

- [ ] Write a validator that requires the two C# components, two shaders, their `.meta` files, and exact `Pinball` lifecycle hooks.
- [ ] Run `python3 Tools/validate_arcane_vfx.py` and confirm it fails before implementation.
- [ ] Commit the failing contract test.

### Task 2: Arcane shaders and pinball-local VFX

**Files:**
- Create: `Assets/Resources/ArcaneVFX/ArcaneSprite.shader`
- Create: `Assets/Resources/ArcaneVFX/ArcaneAdditive.shader`
- Create: `Assets/02. Scripts/Visual/PinballArcaneVfx.cs`
- Modify: `Assets/02. Scripts/Pinball/Pinball.cs`

**Interfaces:**
- Consumes: `Pinball.Velocity`, its root `SpriteRenderer`, and collision contacts.
- Produces: `Initialize(SpriteRenderer, Rigidbody2D)`, `OnActivated()`, `OnDeactivated()`, `OnVelocityChanged(Vector2)`, and `PlayCollision(Vector2, float)`.

- [ ] Implement single-pass `Universal2D` shaders with alpha blending and no screen-texture sampling.
- [ ] Implement `PinballArcaneVfx` so renderers and a capped particle system are created once in `Awake`/`Initialize`.
- [ ] Update `Pinball` to initialize the VFX once and forward lifecycle, velocity, and collision data without changing physics decisions.
- [ ] Run the validator and inspect its remaining failures.
- [ ] Commit the pinball VFX implementation.

### Task 3: Scene look bootstrap

**Files:**
- Create: `Assets/02. Scripts/Visual/ArcaneGameLook.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: active scene name, `Camera.main`, `UniversalAdditionalCameraData`, and the `MapBackground` SpriteRenderer.
- Produces: one global runtime Volume containing Bloom, ColorAdjustments, Tonemapping, and Vignette.

- [ ] Implement a scene-load bootstrap gated to `02. Game` and make repeated setup idempotent.
- [ ] Configure restrained Bloom with high-quality filtering disabled and apply a cool background tint.
- [ ] Set the serialized game-camera post-processing flag to enabled as a safe editor-visible default.
- [ ] Run the validator and confirm all static contracts pass.
- [ ] Commit the scene look.

### Task 4: Packaging and handoff verification

**Files:**
- Create: `ARCANE_VISUAL_SLICE_README.md`
- Create: `pinball-arcane-visual-slice.zip` outside the project root.

**Interfaces:**
- Consumes: completed project and validator output.
- Produces: a Unity-openable ZIP plus explicit Unity/WebGL verification steps.

- [ ] Document changed files, tuning constants, rollback steps, and Unity 6000.0.79f1 verification instructions.
- [ ] Run the validator, scan meta/GUID pairs, and review the Git diff.
- [ ] Package `Assets`, `Packages`, `ProjectSettings`, documentation, and tools while excluding `.git`, caches, and generated build folders.
- [ ] List the ZIP and confirm required project roots are present.
