# Arcane Pinball Core SFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate 18 magical-mechanical WAV assets and connect 12 core SFX event categories to the existing Unity pinball prototype without breaking BGM, mute, mixer, pooling, or current gameplay.

**Architecture:** A Unity Editor-only deterministic synthesizer creates and validates the WAV files so the assets are reproducible without an external package or service. Runtime playback remains in the existing `SoundManager`; a small policy helper owns variant selection, pitch range, and same-category throttling, while existing gameplay and UI classes request sounds only after their events are accepted.

**Tech Stack:** Unity 6 `6000.0.79f1`, C#, Unity Test Framework/NUnit, Unity `AudioClip`/`AudioSource`/`AudioMixer`, Editor scripting, PCM16 mono WAV, PC WebGL

## Global Constraints

- Preserve the existing project structure, gameplay behavior, public APIs, BGM playback, mute controls, mixer routing, and pooled SFX-source lifecycle.
- Add no external package or service and create no new top-level folder.
- Place all 18 audio assets under `Assets/06. Sounds/SFX/`.
- Generate mono PCM16 WAV at exactly 44,100 Hz, with a peak no higher than approximately -3 dBFS and no looping.
- Use `Decompress On Load` for these short SFX unless measured WebGL behavior demonstrates a reason to change it.
- Use scene-placed services and Inspector references; do not introduce runtime service creation.
- New `[SerializeField]` names must not begin with an underscore. Existing underscored fields are not renamed in this scope.
- Repeated wall, bumper, and pin categories use three authored variants, small pitch variation, and a same-category interval between 30 and 50 milliseconds.
- Goal, purchase, victory, and defeat cues use no random pitch.
- Sound playback observes accepted gameplay events and never decides gameplay success.
- Preserve the user's existing uncommitted change in `Assets/01. Scenes/02. Game.unity`; never stage or rewrite it incidentally.

---

## File map

### Create

- `Assets/02. Scripts/Audio/SfxCatalog.cs` — stable runtime keys, variant arrays, category IDs, pitch ranges, and retrigger intervals.
- `Assets/02. Scripts/Audio/SfxPlaybackPolicy.cs` — deterministic, testable category throttling and variant-index selection.
- `Assets/02. Scripts/Audio/Editor/CoreSfxGenerator.cs` — deterministic layered synthesis, PCM16 WAV encoding, file generation, and Unity import configuration.
- `Assets/02. Scripts/Audio/Editor/CoreSfxSetup.cs` — assigns the generated clips to the scene-hosted `SoundManager` without hand-editing YAML.
- `Assets/02. Scripts/Audio/Editor/SfxPlaybackPolicyTests.cs` — edit-mode tests for cooldown and variant selection.
- `Assets/02. Scripts/Audio/Editor/CoreSfxGeneratorTests.cs` — edit-mode tests for asset manifest and WAV format/quality.
- `Assets/06. Sounds/SFX/*.wav` — the 18 approved audio files.

### Modify

- `Assets/02. Scripts/01. Manager/SoundManager.cs` — variant playback, pitch range, and category throttling while preserving `PlaySFX(string)`.
- `Assets/02. Scripts/Pinball/Pinball.cs` — wall and small-pin requests with collision-strength volume scaling.
- `Assets/02. Scripts/Pinball/PinballManager.cs` — successful launch request.
- `Assets/02. Scripts/Pinball/PinballObstacle.cs` — large-bumper request.
- `Assets/02. Scripts/Pinball/PinballReflectorController.cs` — reflector request.
- `Assets/02. Scripts/Pinball/PinballMagnetController.cs` — accepted magnet request.
- `Assets/02. Scripts/Pinball/PinballGoal.cs` — accepted goal request.
- `Assets/02. Scripts/Pinball/PinballOutZone.cs` — accepted out-zone request.
- `Assets/02. Scripts/03. UI/ShopPanel.cs` — successful purchase and accepted reroll click requests.
- `Assets/02. Scripts/03. UI/BottomTabPanel.cs` — accepted primary tab click request.
- `Assets/02. Scripts/03. UI/ResultPanel.cs` — result stingers and return-to-title click.
- `Assets/01. Scenes/00. Developer.unity` — serialized SFX clip registration only.
- AI usage record used by this repository, if one already exists and is discoverable during implementation — factual implementation and verification entry only; do not invent a new tracking system.

---

### Task 1: Runtime SFX catalog and playback policy

**Files:**
- Create: `Assets/02. Scripts/Audio/SfxCatalog.cs`
- Create: `Assets/02. Scripts/Audio/SfxPlaybackPolicy.cs`
- Test: `Assets/02. Scripts/Audio/Editor/SfxPlaybackPolicyTests.cs`

**Interfaces:**
- Consumes: current realtime value supplied by `SoundManager`; Unity random value normalized to `[0, 1)`.
- Produces: `SfxPlaybackPolicy.TrySelect(string category, float now, float minimumInterval, int variantCount, float normalizedRandom, out int variantIndex)`; `readonly struct SfxCue` with `string Category`, `IReadOnlyList<string> Keys`, `float MinimumInterval`, `float MinimumPitch`, and `float MaximumPitch`; and static keys, arrays, and cue values in `SfxCatalog`.

- [ ] **Step 1: Write failing cooldown and variant-selection tests**

```csharp
[Test]
public void TrySelect_BlocksOnlySameCategoryDuringInterval()
{
    var policy = new SfxPlaybackPolicy();

    Assert.That(policy.TrySelect("Wall", 1f, 0.04f, 3, 0f, out _), Is.True);
    Assert.That(policy.TrySelect("Wall", 1.02f, 0.04f, 3, 0.5f, out _), Is.False);
    Assert.That(policy.TrySelect("Bumper", 1.02f, 0.04f, 3, 0.5f, out _), Is.True);
}

[TestCase(0f, 0)]
[TestCase(0.34f, 1)]
[TestCase(0.99f, 2)]
public void TrySelect_MapsNormalizedRandomToVariant(float random, int expected)
{
    var policy = new SfxPlaybackPolicy();
    Assert.That(policy.TrySelect("Pin", 1f, 0f, 3, random, out var actual), Is.True);
    Assert.That(actual, Is.EqualTo(expected));
}

[Test]
public void TrySelect_RejectsEmptyVariantSet()
{
    var policy = new SfxPlaybackPolicy();
    Assert.That(policy.TrySelect("Wall", 1f, 0.04f, 0, 0f, out _), Is.False);
}
```

- [ ] **Step 2: Run the tests and verify the expected compile failure**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter SfxPlaybackPolicyTests -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\sfx-policy-tests.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\sfx-policy-tests.log' -quit
```

Expected: non-zero exit or failed tests because `SfxPlaybackPolicy` does not exist.

- [ ] **Step 3: Implement the minimal policy**

```csharp
using System.Collections.Generic;
using UnityEngine;

public sealed class SfxPlaybackPolicy
{
    private readonly Dictionary<string, float> nextAllowedTimes = new();

    public bool TrySelect(
        string category,
        float now,
        float minimumInterval,
        int variantCount,
        float normalizedRandom,
        out int variantIndex)
    {
        variantIndex = 0;
        if (string.IsNullOrEmpty(category) || variantCount <= 0) return false;
        if (nextAllowedTimes.TryGetValue(category, out var nextAllowed) && now < nextAllowed)
            return false;

        normalizedRandom = Mathf.Clamp(normalizedRandom, 0f, 0.999999f);
        variantIndex = Mathf.FloorToInt(normalizedRandom * variantCount);
        nextAllowedTimes[category] = now + Mathf.Max(0f, minimumInterval);
        return true;
    }
}
```

In `SfxCatalog`, define the exact strings matching the approved filenames without the extension. Use `static readonly string[]` for `WallHits`, `BumperHits`, and `PinHits`, constants for all single cues, `CollisionInterval = 0.04f`, repeated pitch range `0.96f` to `1.04f`, and fixed pitch `1f` for goal/purchase/results. Construct three reusable `SfxCue` values named `WallCue`, `BumperCue`, and `PinCue`; later event-routing code consumes these values without allocating arrays during collisions.

- [ ] **Step 4: Run the focused tests and then all edit-mode tests**

Run the Task 1 command again, then run it without `-testFilter`. Expected: focused tests pass, then the full edit-mode suite passes with zero failures.

- [ ] **Step 5: Commit Task 1 only**

```powershell
git add -- 'Assets/02. Scripts/Audio/SfxCatalog.cs' 'Assets/02. Scripts/Audio/SfxPlaybackPolicy.cs' 'Assets/02. Scripts/Audio/Editor/SfxPlaybackPolicyTests.cs' 'Assets/02. Scripts/Audio/SfxCatalog.cs.meta' 'Assets/02. Scripts/Audio/SfxPlaybackPolicy.cs.meta' 'Assets/02. Scripts/Audio/Editor/SfxPlaybackPolicyTests.cs.meta'
git commit -m "feat: add core sfx playback policy"
```

### Task 2: Deterministic Unity Editor SFX generator

**Files:**
- Create: `Assets/02. Scripts/Audio/Editor/CoreSfxGenerator.cs`
- Create: `Assets/02. Scripts/Audio/Editor/CoreSfxGeneratorTests.cs`

**Interfaces:**
- Consumes: `SfxCatalog` key manifest.
- Produces: `CoreSfxGenerator.OutputDirectory`, `CoreSfxGenerator.ExpectedFileNames`, `CoreSfxGenerator.GenerateAll()`, and `CoreSfxGenerator.ReadWaveInfo(string path)` returning sample rate, channel count, sample count, peak amplitude, leading silence, and trailing silence.

- [ ] **Step 1: Write failing manifest and WAV encoder tests**

```csharp
[Test]
public void ExpectedFileNames_ContainsApprovedEighteenUniqueFiles()
{
    Assert.That(CoreSfxGenerator.ExpectedFileNames, Has.Length.EqualTo(18));
    Assert.That(CoreSfxGenerator.ExpectedFileNames.Distinct().Count(), Is.EqualTo(18));
    Assert.That(CoreSfxGenerator.ExpectedFileNames, Does.Contain("SFX_Pinball_Launch.wav"));
    Assert.That(CoreSfxGenerator.ExpectedFileNames, Does.Contain("SFX_Result_Defeat.wav"));
}

[Test]
public void EncodeWave_WritesMonoPcm16At44100Hz()
{
    var samples = new[] { 0f, 0.25f, -0.25f, 0f };
    var bytes = CoreSfxGenerator.EncodeWave(samples, 44100);
    var info = CoreSfxGenerator.ReadWaveInfo(bytes);

    Assert.That(info.SampleRate, Is.EqualTo(44100));
    Assert.That(info.Channels, Is.EqualTo(1));
    Assert.That(info.BitsPerSample, Is.EqualTo(16));
    Assert.That(info.SampleCount, Is.EqualTo(samples.Length));
}
```

- [ ] **Step 2: Run the focused generator tests and verify failure**

Use the Task 1 Unity command with `-testFilter CoreSfxGeneratorTests`. Expected: compile failure because `CoreSfxGenerator` does not exist.

- [ ] **Step 3: Implement deterministic synthesis primitives and WAV encoding**

Implement these internal primitives in `CoreSfxGenerator`: sine oscillator with linear frequency sweep, seeded white noise, one-pole low-pass filter, exponential decay envelope, linear attack/release envelope, layer mixing, peak normalization to `0.70f` (about -3.1 dBFS), and PCM16 little-endian RIFF encoding. Seed every cue from its filename so repeated generation is byte-identical.

Use the following layer recipes:

- Launch: spring noise burst, low metallic transient, 320-to-760 Hz rising sine, 0.55 seconds.
- Wall variants: 1.7/2.0/2.3 kHz metallic transient plus filtered noise, 0.12 seconds.
- Bumper variants: 130/150/170 Hz body, 600-to-1,100 Hz energy sweep, metallic click, 0.34 seconds.
- Pin variants: 1.3/1.55/1.8 kHz crystal ping plus octave partial, 0.22 seconds.
- Reflector: 900 Hz snap plus 1.8-to-0.9 kHz directional sweep, 0.28 seconds.
- Magnet: 90 Hz suction body plus 240-to-520 Hz swirl and filtered noise, 0.42 seconds.
- Goal: C5-E5-G5-C6 rising arpeggio with a short violet tail, 0.85 seconds.
- Out: 420-to-130 Hz fall plus a soft metallic decay, 0.65 seconds.
- UI click: 1.1 kHz dry tap plus a quiet 2.2 kHz partial, 0.09 seconds.
- Purchase: 880 Hz coin transient plus E5-G5-C6 confirmation tones, 0.58 seconds.
- Victory: C5-E5-G5-C6 chord/arpeggio with restrained shimmer, 1.8 seconds.
- Defeat: D4-A3-D3 descending partials with low metal resonance, 2.0 seconds.

Ensure the first and last sample are zero, add no DC offset, and keep every generated sample within `[-0.70f, 0.70f]`.

- [ ] **Step 4: Implement `GenerateAll()` and Unity import settings**

`GenerateAll()` creates only `Assets/06. Sounds/SFX/`, writes all 18 files, calls `AssetDatabase.Refresh()`, and configures each `AudioImporter`: `forceToMono = true`, `loadInBackground = false`, `preloadAudioData = true`, default sample settings with `loadType = AudioClipLoadType.DecompressOnLoad`, `compressionFormat = AudioCompressionFormat.PCM`, and `sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate`. Expose menu item `Tools/Pin-Ball/Audio/Generate Core SFX` and batch entry point `CoreSfxGenerator.GenerateAll`.

- [ ] **Step 5: Run generator tests and commit the generator**

Run the focused generator tests and then the full edit-mode suite. Expected: zero failures. Commit only generator source, tests, and their Unity `.meta` files.

```powershell
git commit -m "feat: add deterministic core sfx generator"
```

### Task 3: Generate and validate the 18 WAV assets

**Files:**
- Create: `Assets/06. Sounds/SFX/SFX_Pinball_Launch.wav`
- Create: all other approved WAV files listed in the design spec, plus Unity-generated `.meta` files.
- Test: `Assets/02. Scripts/Audio/Editor/CoreSfxGeneratorTests.cs`

**Interfaces:**
- Consumes: `CoreSfxGenerator.GenerateAll()`.
- Produces: 18 imported `AudioClip` assets available to scene setup.

- [ ] **Step 1: Extend the failing generator test to require every on-disk asset**

```csharp
[Test]
public void GeneratedAssets_MeetFormatPeakAndSilenceLimits()
{
    foreach (var fileName in CoreSfxGenerator.ExpectedFileNames)
    {
        var path = Path.Combine(CoreSfxGenerator.OutputDirectory, fileName);
        Assert.That(File.Exists(path), Is.True, fileName);
        var info = CoreSfxGenerator.ReadWaveInfo(path);
        Assert.That(info.SampleRate, Is.EqualTo(44100), fileName);
        Assert.That(info.Channels, Is.EqualTo(1), fileName);
        Assert.That(info.BitsPerSample, Is.EqualTo(16), fileName);
        Assert.That(info.PeakAmplitude, Is.LessThanOrEqualTo(0.71f), fileName);
        Assert.That(info.LeadingSilenceSeconds, Is.LessThan(0.01f), fileName);
        Assert.That(info.TrailingSilenceSeconds, Is.LessThan(0.03f), fileName);
    }
}
```

- [ ] **Step 2: Run the asset test and verify it fails because WAV files are absent**

Run the generator test filter. Expected: failure naming the first missing WAV.

- [ ] **Step 3: Generate all assets in batch mode**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UNITY\pin-ball\pin-ball' -executeMethod CoreSfxGenerator.GenerateAll -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\generate-core-sfx.log' -quit
```

Expected: exit code 0 and exactly 18 WAV files under `Assets/06. Sounds/SFX/`.

- [ ] **Step 4: Run format tests and perform listening review**

Run `CoreSfxGeneratorTests`; expected zero failures. Then listen to every file, explicitly checking category separation, harsh transients, obvious clicks, excessive tails, and the three variants in each repeated category. If a cue fails listening review, adjust only its recipe, regenerate all files, and rerun the exact automated checks.

- [ ] **Step 5: Commit generated assets and their `.meta` files**

Stage only `Assets/06. Sounds/SFX/` and the final generator change, then commit:

```powershell
git commit -m "asset: generate core arcane pinball sfx"
```

### Task 4: Extend `SoundManager` without breaking exact-key playback

**Files:**
- Modify: `Assets/02. Scripts/01. Manager/SoundManager.cs`
- Test: `Assets/02. Scripts/Audio/Editor/SfxPlaybackPolicyTests.cs`

**Interfaces:**
- Consumes: `SfxPlaybackPolicy` and `SfxCatalog` arrays.
- Produces: existing `AudioSource PlaySFX(string name)` unchanged; new `AudioSource PlaySFXVariant(string category, IReadOnlyList<string> names, float minimumInterval, float minimumPitch = 1f, float maximumPitch = 1f, float volumeScale = 1f)`.

- [ ] **Step 1: Add a failing source-configuration test**

Create a test fixture that constructs a `GameObject`, `AudioSource`, and `SoundManager`, supplies one generated test clip through serialized fields, calls the new method, and asserts the returned source uses the chosen clip, SFX mixer group, clamped volume scale, and pitch inside the supplied range. Add a second assertion that immediate same-category replay returns `null` while another category still plays.

- [ ] **Step 2: Run `SfxPlaybackPolicyTests` and verify compile failure for `PlaySFXVariant`**

Expected: compilation fails because the method is absent.

- [ ] **Step 3: Implement variant playback as a narrow extension**

Add one `SfxPlaybackPolicy` field. Refactor only the common `AudioSource` checkout/configuration lines into a private `PlayClip(AudioClip clip, float pitch, float volumeScale)` method. Keep the existing missing-key error and `PlaySFX(string)` behavior. In `PlaySFXVariant`, reject null/empty lists with one descriptive error, ask the policy to select an index using `Time.unscaledTime` and `Random.value`, resolve the selected key in `_sfxDict`, choose pitch with `Random.Range`, clamp `volumeScale` to `[0f, 1f]`, and return `null` when throttled.

Reset `source.pitch = 1f` in `ReturnToPool` so pitch variation cannot leak into later fixed-pitch cues.

- [ ] **Step 4: Run focused and full edit-mode tests**

Expected: exact-key tests, variant tests, cooldown tests, and the complete edit-mode suite pass.

- [ ] **Step 5: Commit the focused manager extension**

```powershell
git add -- 'Assets/02. Scripts/01. Manager/SoundManager.cs' 'Assets/02. Scripts/Audio/Editor/SfxPlaybackPolicyTests.cs'
git commit -m "feat: support throttled sfx variants"
```

### Task 5: Connect pinball gameplay events

**Files:**
- Modify: `Assets/02. Scripts/Pinball/Pinball.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballObstacle.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballReflectorController.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballMagnetController.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballGoal.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballOutZone.cs`
- Test: `Assets/02. Scripts/Audio/Editor/SfxEventRoutingTests.cs`

**Interfaces:**
- Consumes: `App.TryGet<SoundManager>`, `SfxCatalog`, and `SoundManager.PlaySFXVariant`.
- Produces: one SFX request per accepted launch, categorized collision, magnet activation, goal, or out event.

- [ ] **Step 1: Write failing routing tests for pure event classification**

Add `SfxCatalog.GetCollisionCue(EPinballObstacle? obstacle)` returning `Wall`, `Pin`, or `Bumper` metadata. Tests must assert null obstacle maps to wall variants, `SmallPin` maps to pin variants, and `BigBumper` maps to bumper variants. This prevents both `Pinball` and `PinballObstacle` from requesting the bumper sound.

- [ ] **Step 2: Run routing tests and verify failure**

Expected: failure because `GetCollisionCue` and cue metadata do not exist.

- [ ] **Step 3: Implement collision routing with one owner per contact**

In `Pinball.OnCollisionEnter2D`, classify the collider once. Play wall or small-pin variants there, using `Mathf.InverseLerp(1f, maximumSpeed, collision.relativeVelocity.magnitude)` to map volume scale to `Mathf.Lerp(0.55f, 1f, strength)`. Do not play bumper audio there. `PinballObstacle` owns the large-bumper cue after confirming a valid ball and applying bumper velocity.

Use a small private method in each touched component:

```csharp
private static bool TryGetSoundManager(out SoundManager soundManager)
{
    return App.TryGet(out soundManager);
}
```

Do not cache a new Inspector reference on every board object.

- [ ] **Step 4: Connect accepted non-collision events**

- In `PinballManager.TryLaunchLoadedBall`, request launch only after `LaunchLoaded`, successful gold spend, and state update are committed.
- In `PinballReflectorController.OnCollisionEnter2D`, request reflector only after a valid ball receives the impulse.
- In `PinballMagnetController.OnMouseDown`, request magnet only after cooldown acceptance and timestamps are updated.
- In `PinballGoal.OnTriggerEnter2D`, request goal after a valid ball is identified and before handing the accepted event to the manager.
- In `PinballOutZone.OnTriggerEnter2D`, request out after a valid ball is identified and before handing it to the manager.

All fixed cues use `PlaySFX(string)` at pitch `1f`; repeated collision categories use `PlaySFXVariant` with `SfxCatalog.CollisionInterval`, `RepeatedPitchMin`, and `RepeatedPitchMax`.

- [ ] **Step 5: Run edit-mode tests and Unity compilation**

Run the full edit-mode suite. Also run a batch-mode project open with `-quit` and inspect the log for `error CS`, missing scripts, and serialization errors. Expected: zero test failures and zero compilation errors.

- [ ] **Step 6: Commit gameplay event integration**

Stage only the seven pinball scripts and `SfxEventRoutingTests` with its `.meta`; commit:

```powershell
git commit -m "feat: connect pinball core sfx events"
```

### Task 6: Connect purchase, UI, and result events

**Files:**
- Modify: `Assets/02. Scripts/03. UI/ShopPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/BottomTabPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/ResultPanel.cs`
- Test: `Assets/02. Scripts/03. UI/Editor/SfxUiRoutingTests.cs`

**Interfaces:**
- Consumes: `ItemManager.TryPurchase`, `BattleManager` state changes, `App.TryGet<SoundManager>`, and fixed keys in `SfxCatalog`.
- Produces: purchase only on success; UI click on accepted reroll, tab change, and return-to-title; one result stinger per transition into victory or defeat.

- [ ] **Step 1: Write failing result-state routing tests**

Extract a small internal pure selector in `ResultPanel` or `SfxCatalog` and test:

```csharp
[TestCase(EWaveState.Victory, SfxCatalog.Victory)]
[TestCase(EWaveState.Defeat, SfxCatalog.Defeat)]
[TestCase(EWaveState.Pending, null)]
[TestCase(EWaveState.Active, null)]
public void GetResultCue_ReturnsOnlyTerminalStateCue(EWaveState state, string expected)
{
    Assert.That(SfxCatalog.GetResultCue(state), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run the UI routing test and verify failure**

Expected: compile failure because `GetResultCue` is absent.

- [ ] **Step 3: Implement success-aware purchase and UI requests**

Change `ShopPanel.OnPurchaseButtonClicked` to store the boolean returned by `_itemManager.TryPurchase(item)`, play `SfxCatalog.Purchase` only when true, and then refresh. Play `SfxCatalog.UiClick` only after a reroll passes phase/gold validation and before `RerollItems()`. In `BottomTabPanel`, play UI click only when a requested tab is accepted and differs from the active tab. In `ResultPanel.ReturnToTitle`, play UI click immediately before requesting the scene load.

- [ ] **Step 4: Implement one-shot result transition playback**

Track the last handled terminal state in a nonserialized field. When `OnBattleStateChanged` transitions into `Victory` or `Defeat`, resolve the fixed cue and play it once; repeated notification of the same terminal state does not replay. Reset the guard when returning to `Pending` or `Active`.

- [ ] **Step 5: Run focused UI tests, full edit-mode tests, and compile check**

Expected: zero failures and zero C# errors.

- [ ] **Step 6: Commit UI and result integration**

```powershell
git commit -m "feat: connect ui and result sfx events"
```

### Task 7: Register clips in the existing scene-hosted `SoundManager`

**Files:**
- Create: `Assets/02. Scripts/Audio/Editor/CoreSfxSetup.cs`
- Modify: `Assets/01. Scenes/00. Developer.unity`
- Test: `Assets/02. Scripts/Audio/Editor/CoreSfxGeneratorTests.cs`

**Interfaces:**
- Consumes: the 18 imported `AudioClip` assets and existing `SoundManager.Sound` serialized array.
- Produces: exactly 18 unique SFX registrations in the existing Developer-scene `SoundManager`.

- [ ] **Step 1: Add a failing scene registration test**

Open `Assets/01. Scenes/00. Developer.unity` additively in an edit-mode test, locate its single `SoundManager`, inspect `_sfxClips` through `SerializedObject`, and assert 18 entries with unique names and non-null clips whose asset filenames match their keys.

- [ ] **Step 2: Run the scene registration test and verify failure**

Expected: failure because `_sfxClips` is currently empty.

- [ ] **Step 3: Implement idempotent editor setup**

`CoreSfxSetup.ConfigureDeveloperScene()` opens the Developer scene, locates exactly one existing `SoundManager`, replaces only `_sfxClips` with the 18 `SfxCatalog` key/clip pairs, preserves `_bgmClips`, players, mixer, volumes, and pool size, marks the scene dirty, and saves it. Throw a descriptive exception when the scene contains zero or multiple managers or any clip is missing. Expose `Tools/Pin-Ball/Audio/Configure Core SFX` and batch entry point `CoreSfxSetup.ConfigureDeveloperScene`.

- [ ] **Step 4: Run the setup tool and inspect the isolated scene diff**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UNITY\pin-ball\pin-ball' -executeMethod CoreSfxSetup.ConfigureDeveloperScene -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\configure-core-sfx.log' -quit
git diff -- 'Assets/01. Scenes/00. Developer.unity'
```

Expected: only `_sfxClips` gains 18 entries; no BGM, mixer, player, volume, hierarchy, or unrelated scene data changes.

- [ ] **Step 5: Run scene registration and full edit-mode tests**

Expected: 18 valid registrations and zero total failures.

- [ ] **Step 6: Commit setup tool and Developer scene only**

```powershell
git add -- 'Assets/02. Scripts/Audio/Editor/CoreSfxSetup.cs' 'Assets/02. Scripts/Audio/Editor/CoreSfxSetup.cs.meta' 'Assets/01. Scenes/00. Developer.unity'
git commit -m "chore: register core sfx clips"
```

Do not stage `Assets/01. Scenes/02. Game.unity`.

### Task 8: Final runtime, WebGL, and documentation verification

**Files:**
- Modify only if present: the repository's existing AI usage record.
- Do not modify gameplay or audio code during reporting unless a failed verification first produces a separately reviewed fix.

**Interfaces:**
- Consumes: completed Tasks 1-7.
- Produces: fresh test/build evidence, manual listening/play checklist, and factual AI usage record.

- [ ] **Step 1: Run the complete edit-mode test suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\core-sfx-all-tests.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\core-sfx-all-tests.log' -quit
```

Expected: exit code 0, zero failed tests, and no `error CS` lines.

- [ ] **Step 2: Run a PC WebGL development build**

Use the repository's existing build method if present. If none exists, invoke Unity batch mode with a focused Editor build method added alongside `CoreSfxSetup` that builds the enabled scenes to `Temp/WebGLCoreSfxVerification`. Expected: build exit code 0, no audio import errors, and no new runtime exception in the build log. Do not commit the `Temp` build output.

- [ ] **Step 3: Perform the approved play-mode checklist**

In Unity `6000.0.79f1`, verify ten launches; repeated wall, pin, and bumper impacts; both magnets; all four goals; both out zones; one successful and one rejected purchase; victory; defeat; accepted reroll/tab/return clicks; mute toggles; and BGM behavior. Record pass/fail for every item. A rejected purchase must remain silent, every accepted event must request at most one cue, and rapid impacts must not cause audible breakup or source-pool starvation.

- [ ] **Step 4: Perform browser audio verification**

Open the WebGL build, interact once to satisfy browser audio-unlock policy, and repeat launch plus rapid-collision checks. Record whether first-play latency, clipping, breakup, or missing clips occurs.

- [ ] **Step 5: Update the existing AI usage record**

Record the actual model/tool, the user's request and approvals, generated asset paths, exact scripts/scenes changed, user-owned decisions, prompts/instructions that governed the work, automated test counts, WebGL build result, and manual checks. State any check not performed as not performed.

- [ ] **Step 6: Inspect final scope and commit only the record change**

```powershell
git status --short
git diff --check
git log --oneline -8
```

Confirm `Assets/01. Scenes/02. Game.unity` remains an unrelated unstaged user change. Commit the AI record only if a record file existed and was updated:

```powershell
git commit -m "docs: record core sfx implementation"
```

## Final acceptance checklist

- [ ] Exactly 18 approved WAV files exist and import as mono 44.1 kHz PCM/decompressed clips.
- [ ] Peak, silence, uniqueness, and manifest tests pass for every file.
- [ ] All 12 event categories are distinguishable in listening review.
- [ ] Three authored variants are reachable for wall, bumper, and pin categories.
- [ ] Same-category throttling does not suppress unrelated SFX.
- [ ] Accepted events play once; rejected launch/purchase/cooldown actions do not play success cues.
- [ ] Existing exact-key playback, BGM, mute, mixer, and pool-return behavior passes regression checks.
- [ ] Unity edit-mode tests and PC WebGL build pass with fresh evidence.
- [ ] Manual Unity and browser checks are recorded truthfully.
- [ ] The user's unrelated `Assets/01. Scenes/02. Game.unity` change remains preserved and unstaged.
