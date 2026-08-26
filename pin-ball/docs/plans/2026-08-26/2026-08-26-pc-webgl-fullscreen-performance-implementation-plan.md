# PC WebGL Fullscreen Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stabilize Chrome PC WebGL at 2560×1440 fullscreen by applying an evidence-backed render scale tier, 60 FPS frame pacing, and only the UI refresh reductions proven relevant to the current code.

**Architecture:** A pure C# policy resolves the target render scale from screen size and fullscreen state. A scene-placed controller on the persistent `App` object applies the policy only when the screen condition changes and restores the original URP asset value on shutdown. Existing UI controllers retain their event-driven ownership while avoiding identical text and card writes every frame.

**Tech Stack:** Unity 6.0.0.79f1, C#, URP 17.3 2D Renderer, NUnit EditMode tests, Unity Editor scripting, PC WebGL, Chrome.

**Spec:** `docs/designs/2026-08-26/2026-08-26-pc-webgl-fullscreen-performance-design.md` (`1b2d246`)

## Global Constraints

- Preserve pinball 10, enemy 8, ally 5, 2× speed, 60-second empowered assault, and 90-second final assault behavior.
- Target 60 FPS at 1920×1080 and 2560×1440 Chrome fullscreen; treat repeated drops below 50 FPS as a failure condition.
- Use render scale 1.0 at or below 1920×1080 and 0.85 at 2560×1440 fullscreen; evaluate 0.75 only if 0.85 still misses the target in actual measurement.
- Do not change render scale every frame or persist runtime quality changes into `UniversalRP.asset`.
- Do not add packages, runtime-generated UI, broad refactors, new gameplay, or reduced object limits.
- Do not modify or stage the user-owned animation, VFX, or original checkout ProjectSettings changes.
- Work only in the isolated `codex/webgl-fullscreen-performance` worktree until integration.

## File Map

- Create: `Assets/02. Scripts/00. Core/FullscreenRenderScalePolicy.cs` — pure screen-condition policy.
- Create: `Assets/02. Scripts/00. Core/Editor/FullscreenRenderScalePolicyTests.cs` — scale and frame-pacing policy tests.
- Create: `Assets/02. Scripts/00. Core/WebGlFullscreenQualityController.cs` — runtime URP scale application on screen changes.
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualityControllerTests.cs` — application, deduplication, and restore tests.
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualitySceneSetup.cs` — idempotent Developer scene wiring.
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualitySceneTests.cs` — persistent App scene reference checks.
- Modify: `Assets/02. Scripts/00. Core/App.cs` — 60 FPS frame pacing and initial quality application ownership.
- Modify: `Assets/01. Scenes/00. Developer.unity` — pre-place and wire the quality controller on `App`.
- Modify: `Assets/02. Scripts/03. UI/AllyPurchasePanelController.cs` — avoid rewriting unchanged cards each frame while preserving smooth cooldown state.
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs` — update assault countdown only when formatted content changes.
- Modify: existing UI EditMode tests or create `Assets/02. Scripts/03. UI/Editor/WebGlUiRefreshTests.cs` — cache behavior regression coverage.
- Create: `Assets/02. Scripts/Editor/WebGlPerformanceBuild.cs` — repeatable Development and release WebGL build entry points under `.utmp`.
- Update: `docs/ai-usage/2026-08-26/2026-08-26-pc-webgl-fullscreen-performance-implementation-ai-usage.md` — actual measurements, changes, and limitations.

---

### Task 1: Establish the isolated baseline

**Files:**
- Inspect only: all current tracked files
- Output only: `Temp/webgl-performance-baseline.xml`, `Temp/webgl-performance-baseline.log`

**Interfaces:**
- Consumes: commit `9d9ecab` in the isolated worktree.
- Produces: exact baseline test counts and known failures used to distinguish regressions.

- [ ] **Step 1: Verify isolation and cleanliness**

Run `git rev-parse --git-dir`, `git rev-parse --git-common-dir`, `git branch --show-current`, and `git status --short`. Expected: linked worktree on `codex/webgl-fullscreen-performance`, with only this plan untracked.

- [ ] **Step 2: Run the full EditMode baseline**

Run Unity 6 batchmode with `-runTests -testPlatform EditMode` and write XML/log beneath this worktree's `Temp`. Record total, passed, failed, skipped, exit code, compiler errors, Missing Script, and MissingReferenceException. Known failures must be copied exactly rather than summarized as passing.

- [ ] **Step 3: Record static render/UI baseline**

Record the current URP render scale, HDR, MSAA, Canvas reference resolution, App frame settings, and per-frame UI writers in the implementation AI record. No product file changes occur in this step.

- [ ] **Step 4: Commit the plan**

Stage only this plan and commit with `docs: plan WebGL fullscreen optimization`.

---

### Task 2: Add screen-condition and frame-pacing policies with TDD

**Files:**
- Create: `Assets/02. Scripts/00. Core/FullscreenRenderScalePolicy.cs`
- Create: `Assets/02. Scripts/00. Core/FullscreenRenderScalePolicy.cs.meta`
- Create: `Assets/02. Scripts/00. Core/Editor/FullscreenRenderScalePolicyTests.cs`
- Create: `Assets/02. Scripts/00. Core/Editor/FullscreenRenderScalePolicyTests.cs.meta`
- Modify: `Assets/02. Scripts/00. Core/App.cs`

**Interfaces:**
- Produces: `public static float Resolve(int width, int height, bool isFullScreen)`.
- Produces: `public static int TargetFrameRate => 60`.
- Preserves: `QualitySettings.vSyncCount = 1` unless Chrome measurement proves it harmful.

- [ ] **Step 1: Write failing policy tests**

Cover 1920×1080 windowed/fullscreen => 1.0, 2560×1440 windowed => 1.0, 2560×1440 fullscreen => 0.85, larger fullscreen => 0.85, invalid dimensions => 1.0, and target frame rate => 60.

- [ ] **Step 2: Run RED**

Run only `FullscreenRenderScalePolicyTests`. Expected: compile failure because the policy does not exist.

- [ ] **Step 3: Implement the minimum pure policy**

Use explicit constants `FullResolutionScale = 1f`, `QhdFullscreenScale = 0.85f`, `QhdWidth = 2560`, `QhdHeight = 1440`, and no device/GPU guessing.

- [ ] **Step 4: Apply the frame target in App**

Replace the hard-coded target 120 with `FullscreenRenderScalePolicy.TargetFrameRate`. Do not alter service registration, DOTween, scene loading, or persistence.

- [ ] **Step 5: Run GREEN and the App-related regression tests**

Expected: all selected tests pass and compiler error count is zero.

- [ ] **Step 6: Commit**

Stage only the policy, tests, metadata, and `App.cs`; commit `perf(core): target stable WebGL frame pacing`.

---

### Task 3: Apply render scale only when screen conditions change

**Files:**
- Create: `Assets/02. Scripts/00. Core/WebGlFullscreenQualityController.cs`
- Create: `Assets/02. Scripts/00. Core/WebGlFullscreenQualityController.cs.meta`
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualityControllerTests.cs`
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualityControllerTests.cs.meta`

**Interfaces:**
- Consumes: `FullscreenRenderScalePolicy.Resolve(int, int, bool)`.
- Produces: `public void Configure(UniversalRenderPipelineAsset pipelineAsset)` for tests and scene setup.
- Produces: `internal bool ApplyIfChanged(int width, int height, bool isFullScreen)` returning whether the asset value changed.
- Guarantees: original render scale restored on destroy; no write when screen tuple and target scale are unchanged.

- [ ] **Step 1: Write failing controller tests**

Use a temporary URP asset instance. Prove initial 1.0 remains at 1080p, 1440p fullscreen becomes 0.85, repeat input reports no change, windowed input restores 1.0, invalid/null assets fail safely, and shutdown restores the configured original value.

- [ ] **Step 2: Run RED**

Run only `WebGlFullscreenQualityControllerTests`. Expected: compile failure because the controller does not exist.

- [ ] **Step 3: Implement the minimum controller**

Cache width, height, fullscreen, original scale, and last applied scale. Check screen conditions in `Update`, but write to the URP asset only when the tuple changes. Apply in WebGL player builds; expose deterministic input methods for EditMode tests without using runtime UI or object creation.

- [ ] **Step 4: Run GREEN**

Expected: selected tests pass, no compiler errors, and no persistent change to `Assets/Settings/UniversalRP.asset`.

- [ ] **Step 5: Commit**

Stage controller, tests, and metadata only; commit `perf(render): scale QHD fullscreen workload`.

---

### Task 4: Pre-place the persistent quality controller

**Files:**
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualitySceneSetup.cs`
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualitySceneSetup.cs.meta`
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualitySceneTests.cs`
- Create: `Assets/02. Scripts/00. Core/Editor/WebGlFullscreenQualitySceneTests.cs.meta`
- Modify: `Assets/01. Scenes/00. Developer.unity`

**Interfaces:**
- Consumes: `WebGlFullscreenQualityController.Configure(UniversalRenderPipelineAsset)`.
- Produces: one controller on the persistent `App` GameObject with `Assets/Settings/UniversalRP.asset` assigned.

- [ ] **Step 1: Write the failing scene test**

Open Developer scene and assert exactly one `App`, exactly one quality controller on the same GameObject, and a non-null pipeline asset whose serialized scale remains 1.0.

- [ ] **Step 2: Run RED**

Expected: controller component missing from Developer scene.

- [ ] **Step 3: Implement an idempotent setup method**

Add or reuse exactly one component on `App`, assign the existing URP asset, mark the scene dirty only when changed, save once, and never touch Game/Title scenes or ProjectSettings.

- [ ] **Step 4: Execute setup and rerun scene tests**

Expected: scene test passes, a second setup run produces no scene diff, and `UniversalRP.asset` remains unchanged.

- [ ] **Step 5: Commit**

Stage only setup, tests, metadata, and Developer scene; commit `perf(render): wire fullscreen quality policy`.

---

### Task 5: Remove proven redundant UI writes

**Files:**
- Modify: `Assets/02. Scripts/03. UI/AllyPurchasePanelController.cs`
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs`
- Create: `Assets/02. Scripts/03. UI/Editor/WebGlUiRefreshTests.cs`
- Create: matching `.meta` when the test file is new

**Interfaces:**
- Preserves: existing button listeners and battle/unit events.
- Produces: cached displayed card, cooldown second, reinforcement notice, and assault countdown values.
- Guarantees: smooth cooldown masks still update while text/layout writes occur only when the displayed value changes.

- [ ] **Step 1: Write failing cache behavior tests**

Prove identical formatted card/countdown values do not request a second text assignment, a changed cost/count/cooldown/phase does request an update, cooldown masks still reflect fractional remaining time, and state events force a refresh.

- [ ] **Step 2: Run RED**

Expected: cache helpers or observable update decisions do not exist.

- [ ] **Step 3: Implement minimal cached writes**

Keep `Update` only for values that genuinely change continuously. Cache the four rendered card strings, four rounded cooldown seconds, reinforcement visibility/text, and assault countdown formatted string. Assign TMP text and SetActive only when the new value differs.

- [ ] **Step 4: Run GREEN and existing UI scene tests**

Expected: new cache tests and `AllyPurchaseUiSceneTests`, `WaveHudStateTests`, and `GameplayFeedbackSceneTests` match the baseline with no new failures.

- [ ] **Step 5: Commit**

Stage only the two UI controllers and focused tests; commit `perf(ui): skip unchanged HUD writes`.

---

### Task 6: Build, profile, and stop at evidence

**Files:**
- Create: `Assets/02. Scripts/Editor/WebGlPerformanceBuild.cs`
- Create: `Assets/02. Scripts/Editor/WebGlPerformanceBuild.cs.meta`
- Create or update: `docs/ai-usage/2026-08-26/2026-08-26-pc-webgl-fullscreen-performance-implementation-ai-usage.md`
- Output only: `.utmp/WebGLPerformanceDevelopment`, `.utmp/WebGLPerformanceRelease`, and Temp logs/results

**Interfaces:**
- Produces: `public static void BuildDevelopment()` and `public static void BuildRelease()` using enabled build scenes and isolated output directories.
- Does not modify: `EditorBuildSettings.asset`, `ProjectSettings.asset`, build profiles, or published `docs/` WebGL output.

- [ ] **Step 1: Add the focused build entry points**

Use `BuildPipeline.BuildPlayer` with enabled scenes, WebGL target, Development/ConnectWithProfiler flags only for development, and throw on non-succeeded `BuildReport.summary.result`.

- [ ] **Step 2: Run related and full EditMode tests**

Record exact totals and compare every failure with Task 1 baseline. Search logs for compiler errors, Compilation failed, Missing Script, MissingReferenceException, and null serialized references.

- [ ] **Step 3: Build Development WebGL**

Build into `.utmp/WebGLPerformanceDevelopment`; record exit code, duration, size, warnings, and errors. Do not overwrite the repository's published WebGL build.

- [ ] **Step 4: Profile Chrome at fixed scenarios**

Serve the build locally and record 1080p windowed, 1080p fullscreen, and 1440p fullscreen results under the same max-load battle. Compare render scale 1.0 and 0.85 at 1440p. Record FPS/frame-time ranges, GC, memory trend, console errors, UI clarity, input coordinates, and fullscreen transition behavior. If actual Chrome automation or Profiler attachment is unavailable, mark it not performed and provide exact manual steps; never claim the FPS target passed.

- [ ] **Step 5: Apply no speculative second optimization**

If 0.85 meets the target, stop. If it does not, rank the captured CPU/GPU/UI/VFX evidence and write a separate follow-up plan for the single largest confirmed bottleneck. Do not fold 0.75, HDR removal, VFX removal, physics changes, or broad Unit refactors into this plan without recorded evidence.

- [ ] **Step 6: Build release WebGL and record results**

Build into `.utmp/WebGLPerformanceRelease`; verify exit code, browser startup, fullscreen toggle, UI/input, and Chrome console.

- [ ] **Step 7: Complete the AI usage record and final diff audit**

Record actual commands, counts, measurements, limitations, changed files, user decisions, and direct verification steps. Verify original user-owned files never appear in this branch diff.

- [ ] **Step 8: Commit**

Stage the build entry point, metadata, and implementation record only; commit `docs: record WebGL fullscreen optimization`.

## Stop Condition

Stop this milestone after the 0.85 QHD fullscreen policy, 60 FPS pacing, focused redundant UI write reductions, builds, and evidence report are complete. Any additional quality reduction or subsystem optimization requires a new evidence-backed milestone.
