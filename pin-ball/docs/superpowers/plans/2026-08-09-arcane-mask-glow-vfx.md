# Arcane Mask Glow and VFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 분리된 마스크 및 VFX PNG를 사용해 공과 핀볼 장치에 중간 강도의 발광과 작동 피드백을 적용한다.

**Architecture:** 원본 SpriteRenderer는 유지하고, 명도 기반 마스크를 읽는 additive 자식 레이어를 정확히 정렬한다. 공 효과는 기존 `PinballArcaneVfx`, 장치 효과는 공용 `ArcaneMaskGlowController`, 사건별 VFX는 재사용 가능한 `ArcaneSpriteEffect`가 담당한다.

**Tech Stack:** Unity 6, C#, URP 2D, HLSL, SpriteRenderer, MaterialPropertyBlock, NUnit EditMode tests

## Global Constraints

- GAME 씬에 사용자가 배치한 Transform과 Collider를 변경하지 않는다.
- 평상시 발광은 중간 강도, 작동 순간에만 강한 펄스를 사용한다.
- 런타임 효과 오브젝트는 최초 생성 후 재사용하고 Instantiate/Destroy를 반복하지 않는다.
- 효과 에셋 누락은 게임 기능을 중단시키지 않는다.
- PC WebGL에서 지원되는 URP 2D 셰이더 기능만 사용한다.

---

### Task 1: 명도 기반 마스크 발광 코어

**Files:**
- Modify: `Assets/Resources/ArcaneVFX/ArcaneAdditive.shader`
- Create: `Assets/02. Scripts/Visual/ArcaneMaskGlowController.cs`
- Create: `Assets/02. Scripts/Visual/ArcaneVfxCatalog.cs`
- Create: `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset`
- Test: `Assets/02. Scripts/Pinball/Editor/ArcaneGlowMathTests.cs`

**Interfaces:**
- Consumes: 원본 `SpriteRenderer`, 마스크 `Sprite`
- Produces: `Initialize(SpriteRenderer source, Sprite mask)`, `Pulse(float intensity, float duration)`, `SetActiveIntensity(float intensity)`

- [ ] **Step 1: Write the failing alignment and pulse tests**

```csharp
[Test]
public void CalculateMaskScale_MatchesSourceWorldBounds()
{
    Assert.That(ArcaneGlowMath.CalculateMaskScale(new Vector2(2, 1), new Vector2(1, 2)),
        Is.EqualTo(new Vector2(2, 0.5f)));
}

[Test]
public void EvaluatePulse_ReturnsBaseAfterDuration()
{
    Assert.That(ArcaneGlowMath.EvaluatePulse(0.8f, 2f, 0.2f, 0.3f), Is.EqualTo(0.8f));
}
```

- [ ] **Step 2: Run tests and confirm the new APIs are missing**

Run in Unity Test Runner: `EditMode → ArcaneGlowMathTests`
Expected: FAIL because `ArcaneGlowMath` does not exist.

- [ ] **Step 3: Implement mask luminance sampling and aligned overlay**

```hlsl
half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
half luminance = dot(mask.rgb, half3(0.2126, 0.7152, 0.0722));
half strength = luminance * mask.a * input.color.a;
return half4(input.color.rgb * _Intensity * strength, strength);
```

`ArcaneMaskGlowController` creates one child SpriteRenderer, copies flip/sorting data, scales mask bounds to source bounds, and changes `_Intensity` through a MaterialPropertyBlock. `ArcaneVfxCatalog` is a Resources ScriptableObject containing serialized references to the existing mask/VFX sprites, so source PNG files are not moved or duplicated.

- [ ] **Step 4: Run EditMode tests**

Run: `ArcaneGlowMathTests`
Expected: both tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/Resources/ArcaneVFX/ArcaneAdditive.shader Assets/02.\ Scripts/Visual/ArcaneMaskGlowController.cs Assets/02.\ Scripts/Pinball/Editor/ArcaneGlowMathTests.cs
git commit -m "feat: add mask-driven arcane glow"
```

### Task 2: 공 전용 이미지 VFX

**Files:**
- Modify: `Assets/02. Scripts/Visual/PinballArcaneVfx.cs`
- Create: `Assets/02. Scripts/Visual/ArcaneSpriteEffect.cs`
- Test: `Assets/02. Scripts/Pinball/Editor/ArcaneSpriteEffectTests.cs`

**Interfaces:**
- Consumes: `ball_arcane_mask`, `vfx_ball_trail`, `vfx_ball_impact`, `vfx_ball_ring`
- Produces: 기존 `Initialize`, `OnActivated`, `OnDeactivated`, `OnVelocityChanged`, `PlayCollision` 계약 유지

- [ ] **Step 1: Write the failing lifetime test**

```csharp
[Test]
public void NormalizedLifetime_ReachesOneAtDuration()
{
    Assert.That(ArcaneSpriteEffect.NormalizedLifetime(0.25f, 0.25f), Is.EqualTo(1f));
}
```

- [ ] **Step 2: Run test and confirm missing effect class**

Run: `ArcaneSpriteEffectTests`
Expected: FAIL because `ArcaneSpriteEffect` does not exist.

- [ ] **Step 3: Replace procedural feedback with supplied sprites**

`PinballArcaneVfx` reads sprites from `ArcaneVfxCatalog.Load()`, initializes a ball mask glow, uses `vfx_ball_trail` as TrailRenderer texture, and reuses one impact plus one expanding ring SpriteRenderer per pooled ball.

- [ ] **Step 4: Run tests and inspect one launch**

Expected: test PASS; one ball creates a cyan trail and collision impact/ring without adding new hierarchy objects on repeated collisions.

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/02.\ Scripts/Visual/PinballArcaneVfx.cs Assets/02.\ Scripts/Visual/ArcaneSpriteEffect.cs Assets/02.\ Scripts/Pinball/Editor/ArcaneSpriteEffectTests.cs
git commit -m "feat: use supplied ball vfx sprites"
```

### Task 3: 범퍼·자석·반사핀 발광 및 VFX 연결

**Files:**
- Modify: `Assets/02. Scripts/Pinball/PinballObstacle.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballMagnetController.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballReflectorController.cs`
- Modify: `Assets/02. Scripts/Pinball/Editor/ArcanePinballBoardSetup.cs`

**Interfaces:**
- Consumes: `ArcaneMaskGlowController`, `ArcaneSpriteEffect`
- Produces: 범퍼 충돌 펄스, 자석 아크/스파크, 반사핀 작동 펄스

- [ ] **Step 1: Add event assertions to controller tests**

```csharp
[Test]
public void DevicePulse_RaisesGlowIntensity()
{
    var source = new GameObject("bumper").AddComponent<SpriteRenderer>();
    var glow = source.gameObject.AddComponent<ArcaneMaskGlowController>();
    glow.Initialize(source, maskSprite);
    glow.Pulse(2f, 0.2f);
    Assert.That(glow.CurrentIntensity, Is.EqualTo(2f));
}
```

- [ ] **Step 2: Run tests and confirm no glow is triggered**

Expected: FAIL because existing controllers do not initialize or pulse glow.

- [ ] **Step 3: Connect device masks and event effects**

```csharp
private void Awake()
{
    glow = ArcaneMaskGlowController.Attach(
        GetComponent<SpriteRenderer>(),
        ArcaneVfxCatalog.Load().GetMaskFor(gameObject.name));
}
```

범퍼는 `OnCollisionEnter2D`, 자석은 `OnMouseDown`, 반사핀은 자동 flick 시작 시 펄스를 호출한다. 자석은 아크와 스파크 SpriteRenderer를 각 한 개 재사용한다.

- [ ] **Step 4: Run tests and mouse interaction check**

Expected: 범퍼·반사핀 펄스 및 자석 작동 중 아크/스파크가 표시되고 물리 수치는 변하지 않는다.

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/02.\ Scripts/Pinball Assets/02.\ Scripts/Visual
git commit -m "feat: connect device glow feedback"
```

### Task 4: 골 룬 발광과 골인 VFX

**Files:**
- Modify: `Assets/02. Scripts/Pinball/PinballGoal.cs`
- Modify: `Assets/02. Scripts/Pinball/Editor/ArcanePinballBoardSetup.cs`
- Test: `Assets/02. Scripts/Pinball/Editor/PinballGoalVfxTests.cs`

**Interfaces:**
- Consumes: 직업별 룬 마스크, 사방 골 아크, 골 스파크
- Produces: `PlayGoalEffect(Vector2 worldPosition)` 후 기존 `OnGoalBall` 실행

- [ ] **Step 1: Write failing goal effect reuse test**

Create one goal, trigger its effect twice, and assert the number of effect child objects is unchanged after the second trigger.

- [ ] **Step 2: Run test and confirm goal has no effect objects**

Expected: FAIL because `PinballGoal` currently only forwards the ball.

- [ ] **Step 3: Add rune glow and pooled four-arc goal effect**

Initialize the rune mask from `UnitData.UnitId`, create four reusable arc renderers plus one spark renderer, play them before calling `PinballManager.OnGoalBall`, and deactivate them by lifetime.

- [ ] **Step 4: Run EditMode tests and final Play Mode check**

Expected: tests PASS; all four goals show aligned rune glow and entering any goal plays four arcs plus spark exactly once.

- [ ] **Step 5: Final regression check and commit**

Confirm the hierarchy count remains stable after three launches, device positions are unchanged, and Console has no exceptions.

```powershell
git add -- Assets/02.\ Scripts/Pinball/PinballGoal.cs Assets/02.\ Scripts/Pinball/Editor
git commit -m "feat: add arcane goal entry vfx"
```
