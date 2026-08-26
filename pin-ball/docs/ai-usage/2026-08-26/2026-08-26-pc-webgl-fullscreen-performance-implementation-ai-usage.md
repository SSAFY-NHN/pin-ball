# PC WebGL Fullscreen Performance Implementation AI Usage

## Scope and decisions

- Target environment: Chrome, 2560×1440 fullscreen.
- Target frame pacing: 60 FPS; repeated drops below 50 FPS require follow-up evidence.
- Preserve maximum gameplay load: balls 10, enemies 8, allies 5, and 2× speed.
- Use render scale 1.0 through 1920×1080 and 0.85 only at 2560×1440 or larger fullscreen.
- Keep HUD at the existing 1920×1080 Canvas reference resolution.
- Do not introduce the 0.75 tier or another optimization without measurements.
- Work was isolated on `codex/webgl-fullscreen-performance` and did not stage user-owned animation, VFX, or ProjectSettings changes.

## Baseline

- Unity: 6000.0.79f1.
- URP before changes: render scale 1.0, HDR enabled, MSAA 1×.
- Canvas reference resolution: 1920×1080.
- App frame target before changes: 120 FPS with vSync count 1.
- Full EditMode baseline: 297 total, 292 passed, 5 failed, 0 skipped.
- Baseline failures:
  - `BattleDataCharacterizationTests.EnemyCreateStats_AppliesWaveGrowthAndFlooring`
  - `DefenseLineBreachTests.Tick_AfterReinforcementAppears_LeavesDefenseLineAndTargetsAlly`
  - `GameplayFeedbackSceneTests.GameScene_WiresResultCostAndInteractionGlow`
  - `SoundManagerTests.DeveloperScene_RegistersStartupBgmAndEverySfxClip`
  - `UnitCreationServiceTests.TryCreateEnemy_CreatesWaveScaledStats`
- Baseline log contained no C# compiler errors or missing-reference errors.

## Implemented changes

- `FullscreenRenderScalePolicy` owns the deterministic 1080p/QHD scale decision and 60 FPS target.
- `App` now uses the policy's 60 FPS target and retains vSync count 1.
- `WebGlFullscreenQualityController` applies 0.85 only when WebGL screen conditions change and restores the original URP scale when disabled or destroyed.
- The persistent Developer-scene `App` object owns exactly one pre-wired controller referencing `UniversalRP.asset`.
- Ally purchase cards refresh from state changes instead of rebuilding all text every frame.
- Cooldown labels update only when their displayed whole second changes; fill masks retain fractional updates.
- Assault countdown text updates only when its formatted content changes.
- `WebGlPerformanceBuild` provides isolated Development/Profiler and release WebGL build entry points under the project-local ignored `.utmp` directory.

## TDD and focused verification

- Render-scale policy: RED missing-symbol compile failure, then 8/8 passed.
- Quality controller: RED missing class; first GREEN attempt exposed restore behavior; after the minimal lifecycle fix, 4/4 passed.
- Scene wiring: RED missing component, then 1/1 passed; a second setup run produced no scene hash change.
- UI caching: 9/9 focused tests passed.
- Related UI suite: 35 total, 34 passed, 1 failed. The failure was the existing `GameplayFeedbackSceneTests.GameScene_WiresResultCostAndInteractionGlow` baseline failure.
- Build-options tests were written before the implementation. Subsequent Unity invocations reached assembly reload without any `error CS` entries, but could not reach Test Runner completion because the Unity Licensing Client IPC channel repeatedly timed out.

## Verification blocked by local Unity licensing

On the final verification pass, both `-batchmode -nographics` and `-batchmode` stalled in Unity licensing initialization. Logs repeatedly reported `Connection to channel LicenseClient-SSAFY refused`, a 60-second channel timeout, and a failed reconnection. The processes were stopped after repeated identical failures.

Therefore the following were not claimed as completed:

- final focused build-options test result XML;
- final full EditMode regression run;
- Development WebGL build;
- release WebGL build;
- Chrome 1920×1080 and 2560×1440 profiling;
- confirmation that 0.85 meets the 60 FPS target on the user's machine.

## Reproduction after licensing recovery

Run Unity 6000.0.79f1 against this worktree's nested `pin-ball` project without `-quit` for tests:

1. Run `WebGlPerformanceBuildTests`, then the full EditMode suite using `-batchmode -runTests -testPlatform editmode` and explicit `-testResults`/`-logFile` paths.
2. Run `-batchmode -quit -buildTarget WebGL -executeMethod WebGlPerformanceBuild.BuildDevelopment`.
3. Serve `.utmp/WebGLPerformanceDevelopment` locally and profile the fixed max-load scenario in Chrome at 1080p windowed, 1080p fullscreen, and 1440p fullscreen.
4. Compare QHD fullscreen at scale 1.0 and 0.85, recording FPS/frame time, GC, memory trend, console errors, HUD clarity, input alignment, and fullscreen transitions.
5. Run `-batchmode -quit -buildTarget WebGL -executeMethod WebGlPerformanceBuild.BuildRelease` and repeat browser startup, fullscreen, HUD, input, and console checks.

## Commits

- `ecd9892` `perf(core): target stable WebGL frame pacing`
- `00ea633` `perf(render): scale QHD fullscreen workload`
- `e120b1b` `perf(render): wire fullscreen quality policy`
- `e9da901` `perf(ui): skip unchanged HUD writes`
- `80968bc` `build(webgl): add reproducible performance builds`

## Preservation audit

The branch diff from `9d9ecab` contains only the performance policy/controller, their tests and scene wiring, UI caching changes, the isolated build helper, and milestone documentation. `Rabbit1_Mage_Attack.anim`, `ArcaneVfxCatalog.asset`, and ProjectSettings files remain unstaged working-tree modifications and are not part of these commits.
