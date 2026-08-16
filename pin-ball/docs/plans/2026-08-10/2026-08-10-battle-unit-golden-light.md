# Battle Unit Golden Light Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show a Bloom-reactive golden light pool beneath every allied and enemy unit for the complete active-wave duration.

**Architecture:** Add one serialized `GoldenLightPool` SpriteRenderer child to each existing unit prefab and control it with a focused instance component that mirrors `BattleManager.State`. Reuse the existing HDR camera, global Bloom, additive shader, and unit pooling lifecycle; do not add per-unit `Light2D` or runtime-created objects.

**Tech Stack:** Unity 6, C#, URP 2D Renderer, SpriteRenderer, existing `Pinball/ArcaneAdditive` shader, Bloom, NUnit EditMode tests, PC WebGL

## Global Constraints

- Apply the effect to both allied and enemy units.
- Show it only while `EWaveState.Active`; hide it in `Pending`, `Victory`, and `Defeat`.
- Keep the light visible while units move and throughout the active wave.
- Use prefab placement and Inspector references; do not create runtime fallback objects or components.
- Do not add `Light2D`, new `static` members, `Destroy` calls, external packages, or a new top-level folder.
- Preserve unit animation/state tint, combat behavior, pooling, shadows, health bars, and current global Bloom values.
- `[SerializeField]` names must not use underscores.
- Do not modify `Assets/04. Prefabs/UI.prefab` or `Assets/01. Scenes/02. Game.unity`.
- Do not create a git commit unless the user separately requests it.

---

### Task 1: Create and validate the golden light visual assets

**Files:**
- Create: `Assets/03. Images/Effects/GoldenUnitLightPool.png`
- Create: `Assets/03. Images/Effects/GoldenUnitLightPool.png.meta`
- Create: `Assets/09. Materials/Battle.meta` only if the existing `Battle` folder is absent
- Create: `Assets/09. Materials/Battle/GoldenUnitLightPool.mat`
- Create: `Assets/09. Materials/Battle/GoldenUnitLightPool.mat.meta`
- Reuse without modification: `Assets/Resources/ArcaneVFX/ArcaneAdditive.shader`

**Interfaces:**
- Consumes: `Pinball/ArcaneAdditive`, whose SpriteRenderer vertex color supplies gold and whose `_Intensity` value supplies Bloom-reactive HDR output.
- Produces: One transparent white-value mask Sprite and one dedicated additive material for both unit prefabs.

- [ ] **Step 1: Read and invoke the `imagegen` skill**

Use the repository's visual direction and the approved design to create a single reusable raster mask. Generate only this tightly scoped asset: a transparent, horizontally stretched oval pool with a compact white core, stepped soft-gray falloff, clean transparent edges, no baked wide Bloom, no text, no environment, and no character.

Target normalized asset properties:

```text
Path: Assets/03. Images/Effects/GoldenUnitLightPool.png
Canvas: 128 x 64 pixels
Color space: RGBA
Background: fully transparent
Mask values: white/gray only; gold is supplied by SpriteRenderer.color
Composition: centered oval, roughly 104 x 30 pixels, with at least 8 transparent pixels on every edge
```

- [ ] **Step 2: Inspect and normalize the generated Sprite**

Visually inspect the generated image at original resolution. Reject and regenerate it if it contains a colored background, a square halo, scenery, text, asymmetrical perspective, clipped alpha, or broad pre-baked glow.

Configure its Unity import metadata to match a single effects Sprite:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 100
Filter Mode: Point
Compression: None
Alpha Is Transparency: true
Mip Maps: false
Wrap Mode: Clamp
```

- [ ] **Step 3: Create the dedicated additive material**

Create `GoldenUnitLightPool.mat` from the existing `Pinball/ArcaneAdditive` shader with these initial values:

```text
_Intensity: 1.6
_GlowSpread: 1.0
```

Do not modify `ArcaneDeviceAdditive.mat` or the shader. Color and alpha are per-prefab SpriteRenderer values, initially:

```text
Color: HDR-capable warm gold, linear-equivalent starting point approximately (1.00, 0.55, 0.12, 0.32)
```

- [ ] **Step 4: Run asset-level static checks**

Run checks that confirm:

```text
GoldenUnitLightPool.png exists and has nonzero dimensions.
The PNG contains an alpha channel and transparent border pixels.
The material references the existing ArcaneAdditive shader GUID.
No existing shader or global Bloom setting changed.
git diff --check reports no whitespace errors.
```

Expected: all checks pass. If the asset pipeline cannot confirm alpha or dimensions, inspect the PNG through the local image viewer and report that limitation explicitly.

---

### Task 2: Implement state-driven light visibility with tests

**Files:**
- Create: `Assets/02. Scripts/Visual/BattleUnitGoldenLight.cs`
- Create: `Assets/02. Scripts/Visual/BattleUnitGoldenLight.cs.meta`
- Create: `Assets/02. Scripts/Visual/Editor.meta` only if the existing `Editor` folder is absent
- Create: `Assets/02. Scripts/Visual/Editor/BattleUnitGoldenLightTests.cs`
- Create: `Assets/02. Scripts/Visual/Editor/BattleUnitGoldenLightTests.cs.meta`

**Interfaces:**
- Consumes: `BattleManager.State`, `BattleManager.OnStateChanged`, and `EWaveState`.
- Produces: `BattleUnitGoldenLight`, an instance-only prefab component with a serialized `GameObject lightPool`; no new public gameplay API.

- [ ] **Step 1: Write the failing EditMode behavior tests**

Create `BattleUnitGoldenLightTests.cs`. Instantiate a root GameObject and inactive `LightPool` child, add `BattleUnitGoldenLight`, assign `lightPool` through `SerializedObject`, and invoke the component's non-public `ApplyState(EWaveState)` through reflection.

Cover these exact cases:

```csharp
[TestCase(EWaveState.Pending, false)]
[TestCase(EWaveState.Active, true)]
[TestCase(EWaveState.Victory, false)]
[TestCase(EWaveState.Defeat, false)]
public void ApplyState_ShowsLightOnlyDuringActive(
    EWaveState state,
    bool expectedVisible)
```

The test must destroy its temporary root with `Object.DestroyImmediate` in `finally`. This is editor-only test cleanup, not unit lifecycle behavior.

- [ ] **Step 2: Run the targeted test and verify it fails**

Run the Unity EditMode test filter for `BattleUnitGoldenLightTests`.

Expected: compilation or test failure because `BattleUnitGoldenLight` and `ApplyState` do not exist yet.

- [ ] **Step 3: Implement the minimal controller**

Create `BattleUnitGoldenLight.cs` with this responsibility and lifecycle:

```csharp
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleUnitGoldenLight : MonoBehaviour
{
    [SerializeField] private GameObject lightPool;

    private BattleManager battleManager;
    private bool isSubscribed;

    private void Start()
    {
        if (lightPool == null)
        {
            Debug.LogError(
                "[BattleUnitGoldenLight] lightPool is not assigned.",
                this);
            enabled = false;
            return;
        }

        battleManager = App.Get<BattleManager>();
        battleManager.OnStateChanged += ApplyState;
        isSubscribed = true;
        ApplyState(battleManager.State);
    }

    private void OnEnable()
    {
        if (battleManager != null)
        {
            ApplyState(battleManager.State);
        }
    }

    private void ApplyState(EWaveState state)
    {
        if (lightPool != null)
        {
            lightPool.SetActive(state == EWaveState.Active);
        }
    }

    private void OnDestroy()
    {
        if (isSubscribed && battleManager != null)
        {
            battleManager.OnStateChanged -= ApplyState;
        }
    }
}
```

Do not add a runtime fallback, an `Update` loop, a coroutine, a new event, or character SpriteRenderer tinting.

- [ ] **Step 4: Run the targeted tests**

Run the Unity EditMode test filter for `BattleUnitGoldenLightTests`.

Expected: all four state cases pass. Confirm that the test log contains no unexpected missing-reference errors.

---

### Task 3: Serialize the effect into both pooled unit prefabs

**Files:**
- Modify: `Assets/04. Prefabs/AllyUnit.prefab`
- Modify: `Assets/04. Prefabs/EnemyUnit.prefab`
- Create: `Assets/02. Scripts/Visual/Editor/BattleUnitGoldenLightPrefabTests.cs`
- Create: `Assets/02. Scripts/Visual/Editor/BattleUnitGoldenLightPrefabTests.cs.meta`

**Interfaces:**
- Consumes: `BattleUnitGoldenLight`, `GoldenUnitLightPool.png`, and `GoldenUnitLightPool.mat` from Tasks 1–2.
- Produces: Identically configured ally/enemy prefab hierarchies with a serialized state controller and no scene dependency.

- [ ] **Step 1: Write the failing prefab contract tests**

For both paths below, load the prefab with `AssetDatabase.LoadAssetAtPath<GameObject>`:

```text
Assets/04. Prefabs/AllyUnit.prefab
Assets/04. Prefabs/EnemyUnit.prefab
```

Assert all of the following:

```csharp
var controller = prefab.GetComponent<BattleUnitGoldenLight>();
Assert.That(controller, Is.Not.Null);

var light = prefab.transform.Find("GoldenLightPool");
Assert.That(light, Is.Not.Null);
Assert.That(light.gameObject.activeSelf, Is.False);

var renderer = light.GetComponent<SpriteRenderer>();
Assert.That(renderer, Is.Not.Null);
Assert.That(renderer.sprite, Is.Not.Null);
Assert.That(renderer.sharedMaterial, Is.Not.Null);
Assert.That(renderer.sortingOrder, Is.LessThan(
    prefab.GetComponent<SpriteRenderer>().sortingOrder));

var serialized = new SerializedObject(controller);
Assert.That(
    serialized.FindProperty("lightPool").objectReferenceValue,
    Is.SameAs(light.gameObject));
```

- [ ] **Step 2: Run the prefab tests and verify they fail**

Run the Unity EditMode test filter for `BattleUnitGoldenLightPrefabTests`.

Expected: both prefab cases fail because the component and `GoldenLightPool` children have not yet been serialized.

- [ ] **Step 3: Modify only the two existing unit prefabs**

In each prefab:

```text
Add one root component: BattleUnitGoldenLight
Add one root child: GoldenLightPool
Set GoldenLightPool activeSelf: false
Set local position: (0, -0.24, 0)
Set local scale: (1.10, 0.72, 1)
Assign Sprite: GoldenUnitLightPool.png
Assign Material: GoldenUnitLightPool.mat
Set SpriteRenderer color: warm gold with alpha 0.32
Set sorting order: 8
Assign BattleUnitGoldenLight.lightPool to this child
```

Keep `GroundShadow` at sorting order `9` and the character renderer at `10`. Do not edit colliders, root scale, animation frame arrays, labels, unit scripts, or existing material assignments.

- [ ] **Step 4: Run prefab and controller tests**

Run the Unity EditMode filters for `BattleUnitGoldenLightTests` and `BattleUnitGoldenLightPrefabTests`.

Expected: all tests pass. Confirm both prefab YAML files have valid serialized references and no duplicate file IDs.

---

### Task 4: Verify integration, visual quality, and project constraints

**Files:**
- Verify only: all files created or modified in Tasks 1–3
- Update with factual results: `docs/designs/2026-08-10/2026-08-10-battle-unit-golden-light-design.md`

**Interfaces:**
- Consumes: the complete golden-light implementation.
- Produces: verified behavior and an accurate AI-usage/test record; no additional runtime feature.

- [ ] **Step 1: Run automated Unity verification**

Run the targeted golden-light EditMode tests, then the full project EditMode suite.

Expected:

```text
BattleUnitGoldenLightTests: PASS
BattleUnitGoldenLightPrefabTests: PASS
Full EditMode suite: zero failures
Unity script compilation: zero errors
```

If unrelated pre-existing tests fail, record their exact names and errors without modifying unrelated files.

- [ ] **Step 2: Run repository static checks**

Check the changed scope for:

```text
No new production `static` fields or methods.
No new production `Destroy` calls.
No `Light2D` usage.
No runtime `new GameObject()` or `AddComponent()`.
No changes to ArcaneGameLook Bloom values.
No changes to UI.prefab or 02. Game.unity.
No missing or duplicate meta GUIDs/file IDs in changed assets.
git diff --check returns no output.
```

- [ ] **Step 3: Perform manual Play Mode visual checks**

Open `Assets/01. Scenes/02. Game.unity` and verify:

1. Preparation: all existing allies have no golden light.
2. Wave start: every ally and every newly spawned enemy immediately gains the light.
3. Movement: each pool remains centered below its owning unit.
4. Readability: character silhouettes, ground shadows, state tint, and health bars remain readable.
5. Overlap: clustered units do not form a large clipped white patch.
6. Wave end/result: lights disappear outside `Active`.
7. Next wave: persistent allies and reused enemy instances show the correct state again.

If the first-pass tuning is too dim or too bright, adjust only `GoldenUnitLightPool.mat` intensity/spread and the two light SpriteRenderer color/scale values, then repeat this checklist. Do not change global Bloom.

- [ ] **Step 4: Verify representative WebGL behavior**

Run a PC WebGL development build or the project's available WebGL build check. In a representative battle, confirm that the implementation adds only one SpriteRenderer per active unit and no real-time lights. Record build success/failure and any profiler limitation factually.

- [ ] **Step 5: Record AI usage and final observed results**

Append a concise factual implementation record to the approved design document containing:

```text
AI tool/model used
User request and approved decisions
Proposed approach
Actual files changed
User-controlled visual tuning still available
Important constraints followed
Automated/static/manual/WebGL verification actually performed and its result
```

Do not claim Play Mode or WebGL verification if it was not run. Do not commit the changes unless the user separately asks for a commit.
