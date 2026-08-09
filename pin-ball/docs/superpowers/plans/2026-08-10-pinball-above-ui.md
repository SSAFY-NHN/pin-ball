# Pinball Above UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render active pinball bodies and their attached effects above every Game scene UI element.

**Architecture:** Keep the existing Screen Space - Camera Canvas at sorting order 100 and raise the reusable Ball prefab's SpriteRenderer base order to 110. Preserve all effect-relative sorting logic and update the existing board setup utility to use the same base value so regenerated content remains consistent.

**Tech Stack:** Unity 6, C#, SpriteRenderer, TrailRenderer, NUnit EditMode tests

## Global Constraints

- Ball base sorting order is exactly `110`, above the Game Canvas sorting order `100`.
- Existing relative effect offsets remain unchanged.
- Do not change Canvas render mode/order, cameras, physics, UI input, board, obstacles, goals, public APIs, or external packages.
- Preserve the existing project structure and make the smallest verifiable change.

---

### Task 1: Lock and apply the ball rendering order

**Files:**
- Create: `Assets/02. Scripts/Pinball/Editor/PinballRenderingTests.cs`
- Modify: `Assets/04. Prefabs/Ball.prefab:81`
- Modify: `Assets/02. Scripts/Pinball/Editor/ArcanePinballBoardSetup.cs:118`
- Test: `Assets/02. Scripts/Pinball/Editor/PinballRenderingTests.cs`

**Interfaces:**
- Consumes: Existing `Ball.prefab` SpriteRenderer and `UpdatePooledBalls(PinballManager manager)` editor setup flow.
- Produces: A serialized ball sorting-order contract of `110`; no new runtime or public API.

- [ ] **Step 1: Write the failing prefab contract test**

Create `PinballRenderingTests.cs` with:

```csharp
#if UNITY_EDITOR
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PinballRenderingTests
{
    [Test]
    public void BallPrefabSortingOrder_IsGreaterThanGameCanvas()
    {
        var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/04. Prefabs/Ball.prefab");
        Assert.That(ballPrefab, Is.Not.Null);

        var renderer = ballPrefab.GetComponent<SpriteRenderer>();
        Assert.That(renderer, Is.Not.Null);

        var gameScene = EditorSceneManager.OpenScene(
            "Assets/01. Scenes/02. Game.unity", OpenSceneMode.Additive);

        try
        {
            var highestCanvasOrder = gameScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Max(canvas => canvas.sortingOrder);

            Assert.That(renderer.sortingOrder, Is.GreaterThan(highestCanvasOrder));
        }
        finally
        {
            EditorSceneManager.CloseScene(gameScene, true);
        }
    }
}
#endif
```

- [ ] **Step 2: Run the targeted test and verify it fails**

Run the Unity EditMode test filter for `PinballRenderingTests`.

Expected: `BallPrefabSortingOrder_IsGreaterThanGameCanvas` fails because the prefab order `20` is not greater than the Game Canvas order `100`.

- [ ] **Step 3: Apply the minimal serialized and setup changes**

In `Assets/04. Prefabs/Ball.prefab`, change only:

```yaml
m_SortingOrder: 110
```

In `ArcanePinballBoardSetup.UpdatePooledBalls`, change only:

```csharp
renderer.sortingOrder = 110;
```

Do not alter the relative calculations in `PinballArcaneVfx`, `ArcaneMaskGlowController`, or `ArcaneSpriteEffect`.

- [ ] **Step 4: Run automated verification**

Run the targeted `PinballRenderingTests` EditMode test, then the full project EditMode suite.

Expected: The targeted test and all existing EditMode tests pass with zero failures. Run `git diff --check`; expected output is empty.

- [ ] **Step 5: Verify Game view rendering**

Open `Assets/01. Scenes/02. Game.unity`, enter Play Mode, and trigger or move an active ball across UI-covered screen space.

Expected: Ball body, trail, glow, ring, and impact effects render above every panel, image, button, and text element. Board, obstacles, goals, UI interaction, and physics remain unchanged.

- [ ] **Step 6: Update the AI usage record and commit**

Update `docs/superpowers/specs/2026-08-10-pinball-above-ui-design.md` so the modification and verification entries reflect the files actually changed and the observed test results.

```powershell
git add -- "pin-ball/Assets/04. Prefabs/Ball.prefab" "pin-ball/Assets/02. Scripts/Pinball/Editor/ArcanePinballBoardSetup.cs" "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballRenderingTests.cs" "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballRenderingTests.cs.meta" "pin-ball/docs/superpowers/specs/2026-08-10-pinball-above-ui-design.md"
git commit -m "fix: render pinballs above ui"
```
