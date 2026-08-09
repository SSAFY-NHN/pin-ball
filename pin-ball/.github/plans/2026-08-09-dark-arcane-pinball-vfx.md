# Dark Arcane Pinball VFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 전투 동작을 보존하면서 핀볼 한 종류, 장애물 두 종류, 충돌 효과와 선택적 Bloom으로 다크 아케인 네온 수직 슬라이스를 완성한다.

**Architecture:** 기존 `Pinball`이 물리와 게임 규칙을 계속 소유하고, 새 `PinballVisualController`가 Trail 및 충돌 파티클의 표시와 풀링 수명주기만 담당한다. 하나의 WebGL 호환 Unlit 투명 셰이더를 핀볼, Trail, 파티클에 공용으로 사용하고, URP Bloom은 게임 카메라에서만 활성화한다.

**Tech Stack:** Unity 6.0.79f1, C#, URP 17.3.0, Renderer2D, ShaderLab/HLSL, Unity ParticleSystem, TrailRenderer, Unity Test Framework 1.6.0, PC WebGL

## Global Constraints

- 대상은 PC WebGL이며 외부 패키지를 추가하지 않는다.
- 기존 물리, 핀볼 아이템 판정, 전투 규칙 및 공개 API를 변경하지 않는다.
- 핵심 컴포넌트와 효과 오브젝트는 Prefab/Inspector 참조를 사용하고 런타임 `Instantiate`를 사용하지 않는다.
- 핀볼 풀의 `SetActive` 수명주기를 그대로 사용한다.
- 새 최상위 폴더를 만들지 않는다. 머티리얼과 셰이더는 기존 `Assets/09. Materials` 아래에 둔다.
- `[SerializeField]` 필드는 underscore 없이 작성한다.
- Bloom 대상은 핀볼 코어, Trail, 충돌 파티클로 한정하고 일반 UI와 캐릭터는 변경하지 않는다.
- 색 규칙은 핀볼 cyan, SmallPin 충돌 violet, BigBumper 충돌 gold로 고정한다.
- 1차 범위에는 배경, 캐릭터 원화, UI 프레임 교체, 화면 왜곡, 크로매틱 애버레이션을 포함하지 않는다.

---

## File Map

- `Assets/09. Materials/PinballEmissive.shader`: SpriteRenderer, TrailRenderer, ParticleSystem에서 공용으로 쓰는 가산형 Unlit 발광 셰이더.
- `Assets/09. Materials/PinballCore.mat`: cyan HDR 핀볼 코어 머티리얼.
- `Assets/09. Materials/PinballTrail.mat`: 낮은 강도의 cyan Trail 머티리얼.
- `Assets/09. Materials/PinballImpact.mat`: 파티클의 vertex color를 받는 충돌 머티리얼.
- `Assets/02. Scripts/Pinball/PinballVisualController.cs`: Trail 초기화, 충돌 위치/색 선택, 파티클 재생, 풀 복귀 시 초기화.
- `Assets/02. Scripts/Pinball/Pinball.cs`: 기존 충돌·활성화·비활성화 지점에서 비주얼 컨트롤러만 호출.
- `Assets/04. Prefabs/Ball.prefab`: 공용 머티리얼, TrailRenderer, 자식 ParticleSystem 및 Inspector 참조.
- `Assets/DefaultVolumeProfile.asset`: 저품질 Bloom 기준값.
- `Assets/01. Scenes/02. Game.unity`: Main Camera의 post processing 활성화.
- `Assets/Tests/EditMode/PinballVfxAssetTests.cs`: 셰이더, 머티리얼, Prefab 연결, Bloom 설정 회귀 테스트.
- `.github/ai-use-log.md`: 요청, 결정, 수정 범위, 모델 및 검증 결과 기록.

---

### Task 1: WebGL 발광 셰이더와 머티리얼

**Files:**
- Create: `Assets/09. Materials/PinballEmissive.shader`
- Create: `Assets/09. Materials/PinballEmissive.shader.meta`
- Create: `Assets/09. Materials/PinballCore.mat`
- Create: `Assets/09. Materials/PinballTrail.mat`
- Create: `Assets/09. Materials/PinballImpact.mat`
- Test: `Assets/Tests/EditMode/PinballVfxAssetTests.cs`

**Interfaces:**
- Consumes: SpriteRenderer의 `_MainTex`, Renderer vertex color.
- Produces: shader name `PinBall/EmissiveUnlit`; properties `_MainTex`, `_Tint`, `_EmissionStrength`.

- [ ] **Step 1: 셰이더 로드 실패 테스트 작성**

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PinballVfxAssetTests
{
    [Test]
    public void EmissiveShader_IsAvailableAndSupported()
    {
        var shader = Shader.Find("PinBall/EmissiveUnlit");
        Assert.That(shader, Is.Not.Null);
        Assert.That(shader.isSupported, Is.True);
    }

    [TestCase("Assets/09. Materials/PinballCore.mat")]
    [TestCase("Assets/09. Materials/PinballTrail.mat")]
    [TestCase("Assets/09. Materials/PinballImpact.mat")]
    public void VfxMaterial_UsesEmissiveShader(string path)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("PinBall/EmissiveUnlit"));
    }
}
```

- [ ] **Step 2: EditMode 테스트를 실행해 셰이더와 머티리얼 부재로 실패 확인**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter PinballVfxAssetTests -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\pinball-vfx-editmode.xml' -logFile -
```

Expected: `EmissiveShader_IsAvailableAndSupported` 및 머티리얼 테스트 FAIL.

- [ ] **Step 3: 공용 Unlit 발광 셰이더 구현**

핵심 fragment 출력은 텍스처, vertex color, `_Tint`를 곱한 뒤 RGB에만 발광 배수를 적용한다.

```hlsl
half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv)
    * input.color * _Tint;
color.rgb *= _EmissionStrength;
return color;
```

ShaderLab 상태는 `Blend SrcAlpha One`, `ZWrite Off`, `Cull Off`, `Lighting Off`로 고정하고 URP 전용 단일 pass만 둔다. 키워드와 멀티패스는 추가하지 않는다.

- [ ] **Step 4: 세 머티리얼 생성 및 기준값 지정**

- `PinballCore.mat`: `_Tint=(0.25, 1.0, 1.0, 1)`, `_EmissionStrength=2.4`
- `PinballTrail.mat`: `_Tint=(0.15, 0.75, 1.0, 0.65)`, `_EmissionStrength=1.8`
- `PinballImpact.mat`: `_Tint=(1, 1, 1, 1)`, `_EmissionStrength=2.2`; 실제 색은 particle start color에서 결정

- [ ] **Step 5: 테스트 재실행**

Expected: Task 1의 셰이더·머티리얼 테스트 PASS, Console shader compile error 0건.

- [ ] **Step 6: 커밋**

```powershell
git add 'pin-ball/Assets/09. Materials' 'pin-ball/Assets/Tests/EditMode/PinballVfxAssetTests.cs'
git commit -m "feat: add WebGL pinball emissive materials"
```

---

### Task 2: 핀볼 풀링 수명주기와 비주얼 연결

**Files:**
- Create: `Assets/02. Scripts/Pinball/PinballVisualController.cs`
- Modify: `Assets/02. Scripts/Pinball/Pinball.cs:44-123`
- Modify: `Assets/04. Prefabs/Ball.prefab`
- Modify: `Assets/Tests/EditMode/PinballVfxAssetTests.cs`

**Interfaces:**
- Consumes: `Pinball.Activate`, `Pinball.OnCollisionEnter2D`, `Pinball.Deactivate`, `EPinballObstacle`.
- Produces: `PinballVisualController.OnActivated()`, `PlayImpact(Vector2, EPinballObstacle)`, `OnDeactivated()`.

- [ ] **Step 1: Prefab 연결 실패 테스트 추가**

```csharp
[Test]
public void BallPrefab_HasConfiguredVisualController()
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/04. Prefabs/Ball.prefab");
    var visual = prefab.GetComponent<PinballVisualController>();

    Assert.That(visual, Is.Not.Null);
    Assert.That(prefab.GetComponent<TrailRenderer>(), Is.Not.Null);

    var serialized = new SerializedObject(visual);
    Assert.That(serialized.FindProperty("trailRenderer").objectReferenceValue, Is.Not.Null);
    Assert.That(serialized.FindProperty("impactParticles").objectReferenceValue, Is.Not.Null);
}
```

- [ ] **Step 2: 테스트를 실행해 컴포넌트 부재로 실패 확인**

Expected: `BallPrefab_HasConfiguredVisualController` FAIL.

- [ ] **Step 3: `PinballVisualController` 최소 구현**

```csharp
public sealed class PinballVisualController : MonoBehaviour
{
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem impactParticles;
    [SerializeField] private Color smallPinColor = new(0.55f, 0.3f, 1f, 1f);
    [SerializeField] private Color bigBumperColor = new(1f, 0.65f, 0.15f, 1f);

    public void OnActivated()
    {
        trailRenderer.Clear();
        trailRenderer.emitting = true;
    }

    public void PlayImpact(Vector2 worldPosition, EPinballObstacle obstacle)
    {
        impactParticles.transform.position = worldPosition;
        var main = impactParticles.main;
        main.startColor = obstacle == EPinballObstacle.BigBumper
            ? bigBumperColor
            : smallPinColor;
        impactParticles.Play(true);
    }

    public void OnDeactivated()
    {
        trailRenderer.emitting = false;
        trailRenderer.Clear();
        impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
```

`Awake`에서는 Inspector 누락을 자동 생성으로 숨기지 않고 `Debug.Assert`로 참조 누락을 드러낸다.

- [ ] **Step 4: 기존 `Pinball`에 최소 호출 연결**

- `EnsureInitialized()`에서 `GetComponent<PinballVisualController>()`를 캐시한다.
- `Activate()`가 활성화와 물리 초기화를 마친 뒤 `OnActivated()`를 호출한다.
- `OnCollisionEnter2D()`에서 장애물 확인 후 첫 contact point와 obstacle type으로 `PlayImpact`를 호출한다.
- `Deactivate()`에서 `OnDeactivated()`를 호출한다.
- 기존 `_manager.ApplyCollisionRetention`과 `_manager.OnBallHit` 순서 및 조건은 유지한다.

- [ ] **Step 5: `Ball.prefab`에 사전 배치 효과 구성**

- Root `Ball`: `TrailRenderer`, `PinballVisualController` 추가.
- Child `ImpactParticles`: `ParticleSystem`과 `ParticleSystemRenderer` 추가.
- Trail: time `0.16`, width `0.32 -> 0`, min vertex distance `0.08`, world space, 24 이하의 예상 segment.
- Particle: simulation space World, duration `0.2`, loop Off, burst `8`, lifetime `0.18-0.28`, speed `1.3-2.5`, size `0.05-0.13`, max particles `16`.
- Runtime Instantiate/Destroy는 사용하지 않는다.

- [ ] **Step 6: EditMode 테스트와 컴파일 확인**

Expected: Prefab 참조 테스트 PASS, C# compile error 0건.

- [ ] **Step 7: 커밋**

```powershell
git add 'pin-ball/Assets/02. Scripts/Pinball/Pinball.cs' 'pin-ball/Assets/02. Scripts/Pinball/PinballVisualController.cs' 'pin-ball/Assets/04. Prefabs/Ball.prefab' 'pin-ball/Assets/Tests/EditMode/PinballVfxAssetTests.cs'
git commit -m "feat: add pooled pinball trail and impacts"
```

---

### Task 3: 게임 카메라의 선택적 Bloom 활성화

**Files:**
- Modify: `Assets/DefaultVolumeProfile.asset:301-325`
- Modify: `Assets/01. Scenes/02. Game.unity:8921-8967`
- Modify: `Assets/Tests/EditMode/PinballVfxAssetTests.cs`

**Interfaces:**
- Consumes: HDR RGB 값이 1을 초과하는 Task 1 머티리얼.
- Produces: 게임 카메라에서만 동작하는 저품질 Bloom.

- [ ] **Step 1: Volume과 카메라 설정 테스트 추가**

```csharp
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Test]
public void GameScene_EnablesRestrainedBloom()
{
    var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
        "Assets/DefaultVolumeProfile.asset");
    Assert.That(profile.TryGet(out Bloom bloom), Is.True);
    Assert.That(bloom.active, Is.True);
    Assert.That(bloom.intensity.value, Is.InRange(0.45f, 0.8f));
    Assert.That(bloom.highQualityFiltering.value, Is.False);

    var scene = EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
    var camera = Camera.main;
    var cameraData = camera.GetUniversalAdditionalCameraData();
    Assert.That(camera.allowHDR, Is.True);
    Assert.That(cameraData.renderPostProcessing, Is.True);
    EditorSceneManager.CloseScene(scene, true);
}
```

- [ ] **Step 2: 테스트를 실행해 현재 Bloom 강도 0 및 post processing Off로 실패 확인**

Expected: `GameScene_EnablesRestrainedBloom` FAIL.

- [ ] **Step 3: WebGL 기준 Bloom 설정**

- Main Camera: HDR 유지, `renderPostProcessing=true`.
- Bloom threshold `1.0`.
- Bloom intensity `0.6`.
- Bloom scatter `0.55`.
- high quality filtering `false`.
- downscale은 Quarter/가장 저렴한 지원 값으로 설정.
- Lens Distortion, Chromatic Aberration, Film Grain, Depth of Field, Motion Blur는 0 또는 비활성 유지.

- [ ] **Step 4: 테스트 재실행**

Expected: Bloom 및 카메라 설정 테스트 PASS.

- [ ] **Step 5: 커밋**

```powershell
git add 'pin-ball/Assets/DefaultVolumeProfile.asset' 'pin-ball/Assets/01. Scenes/02. Game.unity' 'pin-ball/Assets/Tests/EditMode/PinballVfxAssetTests.cs'
git commit -m "feat: enable restrained WebGL bloom"
```

---

### Task 4: 시각·물리·WebGL 검증과 튜닝

**Files:**
- Modify: `Assets/09. Materials/PinballCore.mat`
- Modify: `Assets/09. Materials/PinballTrail.mat`
- Modify: `Assets/09. Materials/PinballImpact.mat`
- Modify: `Assets/04. Prefabs/Ball.prefab`
- Modify: `Assets/DefaultVolumeProfile.asset`

**Interfaces:**
- Consumes: Tasks 1-3의 완성된 수직 슬라이스.
- Produces: 플레이 및 WebGL에서 검증된 최종 기준값.

- [ ] **Step 1: 전체 EditMode 테스트 실행**

Run: 앞의 Unity EditMode 명령에서 `-testFilter`를 제거한다.

Expected: failed `0`, C# 및 shader compile error `0`.

- [ ] **Step 2: Game 씬에서 핀볼 수명주기 확인**

직접 확인:

1. 준비 단계에서 핀볼이 비활성일 때 Trail/Particle 잔상이 없는지 확인.
2. 핀볼 발사 직후 이전 발사의 Trail이 남지 않는지 확인.
3. SmallPin 충돌은 violet, BigBumper 충돌은 gold인지 확인.
4. 충돌 효과가 물리 속도와 방향을 바꾸지 않는지 확인.
5. SafetyNet reset, SplitCapsule clone, ReleaseBall 이후에도 잔상이 초기화되는지 확인.

- [ ] **Step 3: 가독성 기준 확인**

- 핀볼 코어는 항상 Trail보다 밝다.
- Trail 길이는 핀볼 지름 기준 약 2-4배 범위다.
- Bloom은 UI 텍스트와 일반 캐릭터까지 번지지 않는다.
- 동시에 활성화된 모든 pooled ball에서 Trail과 파티클이 독립적으로 동작한다.

- [ ] **Step 4: WebGL Development Build와 브라우저 프로파일링**

- 해상도 1920x1080과 1280x720에서 각각 확인.
- 최대 활성 핀볼 및 연속 bumper 충돌 상태를 60초 유지.
- 목표: 평균 60 FPS, 최저 50 FPS 이상; VFX 때문에 생기는 managed allocation `0 B/frame`; VFX particle 합계는 핀볼당 `16` 이하.
- 목표 미달 시 순서대로 particle burst `8 -> 5`, Trail time `0.16 -> 0.12`, Bloom intensity `0.6 -> 0.5`, Bloom downscale 유지 여부를 조정한다.

- [ ] **Step 5: Production WebGL 빌드 확인**

Expected: shader pink material 0건, Console error 0건, 핀볼 및 장애물 판정 회귀 0건.

- [ ] **Step 6: 최종 커밋**

```powershell
git add 'pin-ball/Assets/09. Materials' 'pin-ball/Assets/04. Prefabs/Ball.prefab' 'pin-ball/Assets/DefaultVolumeProfile.asset'
git commit -m "perf: tune pinball VFX for WebGL"
```

---

### Task 5: AI 활용 기록

**Files:**
- Create or Modify: `.github/ai-use-log.md`

- [ ] **Step 1: 사실 기반 기록 추가**

다음 내용을 날짜 `2026-08-09` 아래에 기록한다.

- 사용 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: WebGL용 다크 아케인 네온 스타일의 셰이더 및 VFX 적용
- AI 제안: 선택적 Bloom, 공용 Unlit emissive shader, pooled Trail/Particle 수직 슬라이스
- AI 실제 수정 영역: 완료된 Task들의 정확한 파일 목록
- 사용자 결정 필요 영역: 최종 색, Bloom 강도, Trail 길이, 후속 배경/UI 아트 범위
- 중요 지시: 기존 구조 보존, Inspector 참조, SetActive 풀링, 외부 패키지 금지
- 검증 결과: EditMode, Editor Play, WebGL Development/Production 결과와 실제 수치

- [ ] **Step 2: 기록과 실제 변경 내용 대조**

Expected: 수행하지 않은 작업을 완료로 기록하지 않고, 실패한 테스트나 제한점을 그대로 포함한다.

- [ ] **Step 3: 커밋**

```powershell
git add 'pin-ball/.github/ai-use-log.md'
git commit -m "docs: record AI-assisted pinball VFX work"
```

---

## Out of Scope / Follow-up

수직 슬라이스 승인 후 별도 계획으로 진행한다.

1. 전투 배경을 네이비·청록 3-4단계 레이어로 교체.
2. 캐릭터 피격 외곽광 및 상태이상 룬.
3. UI 프레임의 목재·금속·금색 포인트 리소스.
4. 고등급 아이템과 보스 경고용 발광 규칙.
5. 효과 강도 Low/High WebGL 품질 옵션.

## Self-Review Result

- 요구 범위는 핀볼 수직 슬라이스 하나로 제한되어 독립적으로 검토·거절 가능하다.
- 모든 새 runtime 컴포넌트는 명시적인 Prefab/Inspector 참조를 사용한다.
- 외부 패키지, 공개 API 변경, 런타임 생성, 기존 기능 삭제가 없다.
- 자동 테스트가 어려운 시각 품질과 WebGL GPU 비용은 명시적인 수동 검증 기준으로 보완한다.
