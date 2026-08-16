# Pinball Tail Glow Tuning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Slightly strengthen scene Bloom and make the pinball trail thicker and brighter.

**Architecture:** Preserve the runtime Volume and trail implementation. Change only the approved serialized-in-code tuning constants in `ArcaneGameLook` and `PinballArcaneVfx`; do not alter shaders, scene objects, physics, or VFX lifecycle.

**Tech Stack:** Unity 6, C#, Universal Render Pipeline 2D, URP Bloom, TrailRenderer

## Global Constraints

- Bloom intensity changes from `0.95` to `1.1`.
- Bloom threshold remains `0.6`; scatter remains `0.7`.
- Trail width changes from `0.22` to `0.36` into `0.28` to `0.46`.
- Trail material intensity changes from `1.35` to `1.8`.
- Existing trail colors and lifetime remain unchanged.
- Do not modify scene layout, physics, shader structure, VFX catalog, or public APIs.

---

### Task 1: Apply approved Bloom and trail tuning

**Files:**
- Modify: `Assets/02. Scripts/Visual/ArcaneGameLook.cs:68`
- Modify: `Assets/02. Scripts/Visual/PinballArcaneVfx.cs:81,110`
- Test: Unity Editor import/compile log and Game view Play mode

**Interfaces:**
- Consumes: Existing `Bloom.intensity`, `TrailRenderer.widthMultiplier`, and additive material `_Intensity` values.
- Produces: No new interface; only adjusted runtime visual output.

- [ ] **Step 1: Capture the current approved baseline**

Run:

```powershell
rg -n "bloom.intensity|widthMultiplier|trailMaterial.SetFloat" "Assets/02. Scripts/Visual/ArcaneGameLook.cs" "Assets/02. Scripts/Visual/PinballArcaneVfx.cs"
```

Expected: Bloom `0.95`, trail width `0.22` to `0.36`, and trail intensity `1.35`.

- [ ] **Step 2: Apply the minimal tuning change**

Set the existing statements to:

```csharp
bloom.intensity.Override(1.1f);
_trail.widthMultiplier = Mathf.Lerp(0.28f, 0.46f, speed01);
trailMaterial.SetFloat("_Intensity", 1.8f);
```

Do not change adjacent Bloom, trail color, or lifetime values.

- [ ] **Step 3: Verify the exact source values and whitespace**

Run:

```powershell
rg -n "bloom.intensity|widthMultiplier|trailMaterial.SetFloat" "Assets/02. Scripts/Visual/ArcaneGameLook.cs" "Assets/02. Scripts/Visual/PinballArcaneVfx.cs"
git diff --check
```

Expected: The three approved values are present and `git diff --check` reports no errors.

- [ ] **Step 4: Verify Unity compilation and visual behavior**

Stop Play mode, refresh Assets, and enter Play mode again. Inspect the Unity Editor log for `error CS` and `Shader error`.

Expected: No new compile or shader errors. In Game view the scene is slightly brighter, the ball remains readable, the trail is visibly thicker and brighter, and board devices remain distinguishable.

- [ ] **Step 5: Commit only the tuning files after visual approval**

```powershell
git add -- "pin-ball/Assets/02. Scripts/Visual/ArcaneGameLook.cs" "pin-ball/Assets/02. Scripts/Visual/PinballArcaneVfx.cs"
git commit -m "tune: strengthen pinball glow trail"
```
