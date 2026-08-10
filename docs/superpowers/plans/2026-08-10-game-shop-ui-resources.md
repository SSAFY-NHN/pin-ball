# Game Shop UI Resources Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the new shop and item UI sprites to the Game scene, including category-aware shop slots, without changing fonts or the shared UI prefab.

**Architecture:** Store static sprite assignments in `02. Game.unity` and add only the category-to-sprite selection required by `ShopSlot`. Unity `Button` sprite-swap transitions render hover, press, active, and disabled states; Edit Mode tests cover category selection and scene wiring.

**Tech Stack:** Unity 6.0.79f1, C#, Unity UI, NUnit Edit Mode tests, Unity YAML scenes

## Global Constraints

- Modify `Assets/01. Scenes/02. Game.unity`, not `Assets/04. Prefabs/UI.prefab`.
- Do not apply `Assets/07. Fonts/Galmuri11-Bold.ttf`.
- Preserve item selection, pricing, purchase, tab, and tooltip behavior.
- Preserve the user's evolution image and rabbit animation changes.
- Missing visual references must leave the existing image usable without throwing.

---

### Task 1: Category-aware shop slot visuals

**Files:**
- Create: `pin-ball/Assets/02. Scripts/03. UI/Editor/ShopSlotVisualTests.cs`
- Create: `pin-ball/Assets/02. Scripts/03. UI/Editor/ShopSlotVisualTests.cs.meta` (Unity-generated)
- Modify: `pin-ball/Assets/02. Scripts/03. UI/ShopSlot.cs`

**Interfaces:**
- Consumes: `Item.Category : EItemCategory`, `Button.targetGraphic`
- Produces: `ShopSlot.ResolveNormalSprite(EItemCategory, Sprite, Sprite, Sprite) : Sprite`; serialized `ballNormalSprite`, `boardNormalSprite`, `battleNormalSprite`

- [ ] **Step 1: Write the failing category mapping test**

```csharp
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class ShopSlotVisualTests
{
    [TestCase(EItemCategory.Ball, 0)]
    [TestCase(EItemCategory.Board, 1)]
    [TestCase(EItemCategory.Battle, 2)]
    public void ResolveNormalSprite_ReturnsCategorySprite(EItemCategory category, int expected)
    {
        var sprites = new[] {
            Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero),
            Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero),
            Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero)
        };
        Assert.That(ShopSlot.ResolveNormalSprite(category, sprites[0], sprites[1], sprites[2]), Is.SameAs(sprites[expected]));
    }

    [Test]
    public void ResolveNormalSprite_MissingCategorySpriteFallsBackToBall()
    {
        var ball = Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero);
        Assert.That(ShopSlot.ResolveNormalSprite(EItemCategory.Board, ball, null, null), Is.SameAs(ball));
    }
}
#endif
```

- [ ] **Step 2: Run the focused test and verify RED**

```powershell
& $UnityEditor -batchmode -nographics -quit -projectPath "$PWD/pin-ball" -runTests -testPlatform EditMode -testFilter ShopSlotVisualTests -testResults "$PWD/pin-ball/TestResults-shop-slot.xml" -logFile "$PWD/pin-ball/TestLog-shop-slot.txt"
```

Expected: compilation fails because `ShopSlot.ResolveNormalSprite` does not exist.

- [ ] **Step 3: Implement the minimal resolver and use it from `SetItem`**

Add three serialized sprite fields and this method to `ShopSlot`:

```csharp
public static Sprite ResolveNormalSprite(EItemCategory category, Sprite ball, Sprite board, Sprite battle)
{
    var resolved = category switch
    {
        EItemCategory.Board => board,
        EItemCategory.Battle => battle,
        _ => ball
    };
    return resolved != null ? resolved : ball;
}
```

In `SetItem`, assign the resolved non-null sprite to `purchaseButton.targetGraphic` when it is an `Image`. Retain icon, callback, and text behavior unchanged.

- [ ] **Step 4: Re-run the focused test and verify GREEN**

Expected: all `ShopSlotVisualTests` pass.

- [ ] **Step 5: Commit**

```powershell
git add -- 'pin-ball/Assets/02. Scripts/03. UI/ShopSlot.cs' 'pin-ball/Assets/02. Scripts/03. UI/Editor/ShopSlotVisualTests.cs' 'pin-ball/Assets/02. Scripts/03. UI/Editor/ShopSlotVisualTests.cs.meta'
git commit -m "feat: select shop slot art by item category"
```

### Task 2: Wire sprites into the Game scene

**Files:**
- Create: `pin-ball/Assets/02. Scripts/03. UI/Editor/GameShopUiSceneTests.cs`
- Create: `pin-ball/Assets/02. Scripts/03. UI/Editor/GameShopUiSceneTests.cs.meta` (Unity-generated)
- Modify: `pin-ball/Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: scene `BottomTabPanel`, `ShopPanel`, `ShopSlot`, `ShopTooltip`, and `ItemTooltip` components
- Produces: Game-scene-only references to the new sprites under `Assets/03. Images/UI`

- [ ] **Step 1: Write a failing scene wiring test**

Create an Edit Mode test that opens `Assets/01. Scenes/02. Game.unity` using `EditorSceneManager.OpenScene`. Load expected sprites using `AssetDatabase.LoadAssetAtPath<Sprite>` and use `SerializedObject` for private fields. Assert:

```csharp
Assert.That(itemButton.image.sprite, Is.SameAs(sideTabInactive));
Assert.That(itemButton.spriteState.pressedSprite, Is.SameAs(sideTabPressed));
Assert.That(itemButton.spriteState.disabledSprite, Is.SameAs(sideTabActive));
Assert.That(shopButton.image.sprite, Is.SameAs(sideTabInactive));
Assert.That(rerollButton.image.sprite, Is.SameAs(rerollNormal));
Assert.That(rerollButton.spriteState.highlightedSprite, Is.SameAs(rerollHighlighted));
Assert.That(rerollButton.spriteState.pressedSprite, Is.SameAs(rerollPressed));
Assert.That(rerollButton.spriteState.disabledSprite, Is.SameAs(rerollDisabled));
```

Also assert both tab icon images, both tooltip background images, all three serialized category sprites on every `ShopSlot`, and each purchase button's highlighted and disabled sprites.

- [ ] **Step 2: Run the scene test and verify RED**

```powershell
& $UnityEditor -batchmode -nographics -quit -projectPath "$PWD/pin-ball" -runTests -testPlatform EditMode -testFilter GameShopUiSceneTests -testResults "$PWD/pin-ball/TestResults-game-shop-ui.xml" -logFile "$PWD/pin-ball/TestLog-game-shop-ui.txt"
```

Expected: assertions fail because the Game scene still references old sprites.

- [ ] **Step 3: Assign tab and reroll states in `02. Game.unity`**

For `ItemTabButton` and `ShopTabButton`, use Sprite Swap with inactive as normal/highlighted, pressed as pressed, and active as disabled. Assign the matching new tab icon to each child icon image. For the reroll button, assign all four `ui_button_shop_reroll_*` sprites to their matching states.

- [ ] **Step 4: Assign tooltip and shop slot sprites**

Set both `Back_Tooltip` images to `ui_item_tooltip_background`. Assign ball, board, and battle normal fields on every scene `ShopSlot`; assign `ui_shop_slot_highlighted` to highlighted/pressed and `ui_shop_slot_sold_out` to disabled on each purchase button. Adjust only RectTransforms that clip the artwork, preserving anchors and layout hierarchy.

- [ ] **Step 5: Re-run the scene test and verify GREEN**

Expected: `GameShopUiSceneTests` passes and the scene opens without missing references or serialization errors.

- [ ] **Step 6: Verify exclusions and commit**

```powershell
git diff --exit-code -- 'pin-ball/Assets/04. Prefabs/UI.prefab'
git add -- 'pin-ball/Assets/01. Scenes/02. Game.unity' 'pin-ball/Assets/02. Scripts/03. UI/Editor/GameShopUiSceneTests.cs' 'pin-ball/Assets/02. Scripts/03. UI/Editor/GameShopUiSceneTests.cs.meta'
git commit -m "feat: apply shop UI resources to game scene"
```

### Task 3: Full verification and visual smoke check

**Files:**
- Verify: `pin-ball/Assets/01. Scenes/02. Game.unity`
- Verify: `pin-ball/Assets/02. Scripts/03. UI/ShopSlot.cs`
- Verify: both new Edit Mode test files

**Interfaces:**
- Consumes: Task 1 and Task 2 outputs
- Produces: verified integration with unrelated user work intact

- [ ] **Step 1: Run relevant UI Edit Mode tests**

```powershell
& $UnityEditor -batchmode -nographics -quit -projectPath "$PWD/pin-ball" -runTests -testPlatform EditMode -testFilter 'ShopSlotVisualTests|GameShopUiSceneTests|WaveHudStateTests|AllyDeploymentLimitTests' -testResults "$PWD/pin-ball/TestResults-ui.xml" -logFile "$PWD/pin-ball/TestLog-ui.txt"
```

Expected: zero failed tests.

- [ ] **Step 2: Inspect diff boundaries**

```powershell
git status --short
git diff --check
git diff --stat
```

Expected: implementation changes are limited to the Game scene, `ShopSlot`, and tests; user resources, evolution image, animation, font, and `UI.prefab` remain intact.

- [ ] **Step 3: Perform a manual Game scene smoke check when Unity GUI is available**

Check active tab switching, reroll states, category backgrounds, hover and sold-out artwork, and both tooltips at the reference resolution. Any layout correction must remain a Game-scene-only RectTransform change and be followed by the Task 2 test.
