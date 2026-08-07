# Unity Project Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 소환 위치·씬명·URP·미사용 프리팹 문제를 수정하고 전체 EditMode·씬·WebGL 검증과 AI 활용 기록을 완료한다.

**Architecture:** 작은 순수 설정 테스트로 씬명과 위치 계산을 고정하고, Unity Editor API로 URP Global Settings의 missing managed reference를 검사·재생성한다. 승인된 프리팹만 참조 검사를 거쳐 삭제한다. 마지막으로 Unity 6000.0.79f1에서 전체 테스트, Game 씬 import, WebGL 빌드를 순서대로 실행한다.

**Tech Stack:** Unity 6000.0.79f1 Editor API, URP 17.0.4, Unity Test Framework, PC WebGL

## Global Constraints

- Unity 버전은 `6000.0.79f1`로 유지한다.
- URP는 `17.0.4`로 맞추고 신규 패키지는 설치하지 않는다.
- `01. Title.unity`와 웨이브 데이터 구조는 수정하지 않는다.
- 삭제 대상은 승인된 `UI.prefab`, `Pinball.prefab`과 각 meta뿐이다.
- Ball, SmallPin, BigBumper 프리팹은 반드시 유지한다.
- 전역 포매팅, 폴더 이동, 파일명 변경은 하지 않는다.
- plan 01~03의 테스트가 모두 통과한 상태에서 시작한다.

---

### Task 1: 명시 소환 위치와 Developer 씬명 수정

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs`
- Modify: `Assets/02. Scripts/01. Manager/SceneManager.cs`
- Create: `Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs`

**Interfaces:**
- Preserves: `UnitSpawner.SpawnEnemy(..., Vector3? spawnPosition)`
- Preserves: `SceneManager.Load(ESceneName)`
- Produces: private deterministic `ApplyFormationOffset(...)` covered through reflection

- [ ] **Step 1: Write failing configuration tests**

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ProjectConfigurationTests
{
    [Test]
    public void ExplicitSpawnPositionDoesNotReceiveFormationOffset()
    {
        var method = typeof(UnitSpawner).GetMethod(
            "ApplyFormationOffset",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var input = new Vector3(3f, 4f, 0f);
        var result = (Vector3)method.Invoke(null, new object[] { input, 7, true });
        Assert.That(result.y, Is.EqualTo(4f));
    }

    [Test]
    public void DeveloperSceneNameMatchesAsset()
    {
        var go = new GameObject("SceneManagerTest");
        var manager = go.AddComponent<SceneManager>();
        var method = typeof(SceneManager).GetMethod(
            "GetSceneName",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var result = (string)method.Invoke(
            manager,
            new object[] { ESceneName.Developer });

        Assert.That(result, Is.EqualTo("00. Developer"));
        Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: Run focused tests and confirm failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter ProjectConfigurationTests -testResults "Temp/project-config-tests.xml" -logFile "Temp/project-config-tests.log"
```

Expected: the spawn helper is absent and Developer maps to `00 .Developer`.

- [ ] **Step 3: Separate default formation from explicit placement**

```csharp
private static Vector3 ApplyFormationOffset(
    Vector3 position,
    int spawnIndex,
    bool hasOverridePosition)
{
    if (!hasOverridePosition)
    {
        position.y += GetFormationOffset(spawnIndex);
    }

    return position;
}
```

The existing small random X offset remains for both normal and reinforcement spawns. Call this helper with `overridePosition.HasValue` before Instantiate.

- [ ] **Step 4: Correct only the Developer string literal**

Change `ESceneName.Developer => "00 .Developer"` to `ESceneName.Developer => "00. Developer"`. Leave all other mappings and transitions unchanged.

- [ ] **Step 5: Run focused tests and verify pass**

Run the Step 2 command. Expected: both tests pass.

- [ ] **Step 6: Commit the two defect fixes**

```powershell
git add -- "Assets/02. Scripts/Battle/UnitSpawner.cs" "Assets/02. Scripts/01. Manager/SceneManager.cs" "Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs"
git commit -m "fix: preserve explicit spawns and developer scene mapping"
```

### Task 2: URP를 Unity 6000.0.79f1 호환 버전으로 복구

**Files:**
- Modify: `Packages/manifest.json`
- Modify if resolver changes it: `Packages/packages-lock.json`
- Modify or regenerate: `Assets/UniversalRenderPipelineGlobalSettings.asset`
- Create temporarily: `Assets/02. Scripts/Editor/UrpGlobalSettingsRepair.cs`
- Modify: `Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs`
- Delete after execution: repair script and generated `.meta`

**Interfaces:**
- Sets: `com.unity.render-pipelines.universal` exactly `17.0.4`
- Verifies: no missing managed-reference types in URP global settings

- [ ] **Step 1: Add failing package and asset tests**

```csharp
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering.Universal;

[Test]
public void ManifestUsesUnity6000CompatibleUrp()
{
    var manifest = File.ReadAllText(Path.Combine(
        Application.dataPath,
        "../Packages/manifest.json"));
    StringAssert.Contains(
        "\"com.unity.render-pipelines.universal\": \"17.0.4\"",
        manifest);
}

[Test]
public void UrpGlobalSettingsHasNoMissingManagedReferences()
{
    var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineGlobalSettings>(
        "Assets/UniversalRenderPipelineGlobalSettings.asset");
    Assert.That(asset, Is.Not.Null);
    Assert.That(
        SerializationUtility.HasManagedReferencesWithMissingTypes(asset),
        Is.False);
}
```

- [ ] **Step 2: Run the two tests and retain failure evidence**

Run Task 1 Step 2. Expected: manifest version test fails at `17.3.0`; global settings may fail or log missing URP types.

- [ ] **Step 3: Align manifest and resolve packages**

Change only the URP manifest dependency to `17.0.4`, then run:

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -quit -logFile "Temp/urp-resolve.log"
```

Expected: `packages-lock.json` reports URP/core/shadergraph 17.0.4 and universal-config 17.0.3, which is the declared dependency of URP 17.0.4.

- [ ] **Step 4: Create and run an exact Global Settings repair method**

```csharp
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class UrpGlobalSettingsRepair
{
    public static void Repair()
    {
        const string path =
            "Assets/UniversalRenderPipelineGlobalSettings.asset";
        var current = AssetDatabase
            .LoadAssetAtPath<UniversalRenderPipelineGlobalSettings>(path);

        if (current != null &&
            SerializationUtility.HasManagedReferencesWithMissingTypes(current))
        {
            AssetDatabase.DeleteAsset(path);
        }

        var ensure = typeof(UniversalRenderPipelineGlobalSettings).GetMethod(
            "Ensure",
            BindingFlags.NonPublic | BindingFlags.Static);
        var settings = ensure?.Invoke(null, new object[] { true });
        if (settings == null)
        {
            throw new System.InvalidOperationException(
                "URP Global Settings Ensure failed.");
        }

        AssetDatabase.SaveAssets();
    }
}
```

Run with `-executeMethod UrpGlobalSettingsRepair.Repair`, exit code 0, then delete the temporary script and meta using `apply_patch`.

- [ ] **Step 5: Rerun project configuration tests and inspect resolver log**

Expected: both URP tests pass and `Temp/urp-resolve.log` contains no missing managed-reference type or package version conflict.

- [ ] **Step 6: Commit URP compatibility assets**

```powershell
git add -- "Packages/manifest.json" "Packages/packages-lock.json" "Assets/UniversalRenderPipelineGlobalSettings.asset" "Assets/UniversalRenderPipelineGlobalSettings.asset.meta" "Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs"
git commit -m "fix: align URP with Unity 6000.0"
```

### Task 3: 승인된 미사용 프리팹만 삭제

**Files:**
- Delete: `Assets/04. Prefabs/UI.prefab`
- Delete: `Assets/04. Prefabs/UI.prefab.meta`
- Delete: `Assets/04. Prefabs/Pinball.prefab`
- Delete: `Assets/04. Prefabs/Pinball.prefab.meta`
- Modify: `Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs`

**Interfaces:**
- Preserves: `Ball.prefab`, `SmallPin.prefab`, `BigBumper.prefab`

- [ ] **Step 1: Add failing asset-presence tests**

```csharp
[Test]
public void ObsoleteAggregatePrefabsAreRemoved()
{
    Assert.That(
        AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/04. Prefabs/UI.prefab"),
        Is.Null);
    Assert.That(
        AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/04. Prefabs/Pinball.prefab"),
        Is.Null);
}

[TestCase("Assets/04. Prefabs/Ball.prefab")]
[TestCase("Assets/04. Prefabs/SmallPin.prefab")]
[TestCase("Assets/04. Prefabs/BigBumper.prefab")]
public void ReferencedChildPrefabsRemain(string path)
{
    Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path), Is.Not.Null);
}
```

- [ ] **Step 2: Run focused tests and confirm obsolete assets still fail**

Expected: only `ObsoleteAggregatePrefabsAreRemoved` fails.

- [ ] **Step 3: Resolve exact GUIDs and verify no retained reference**

```powershell
$uiGuid = (Select-String -Path "Assets/04. Prefabs/UI.prefab.meta" -Pattern '^guid:').Line.Split(':')[1].Trim()
$pinballGuid = (Select-String -Path "Assets/04. Prefabs/Pinball.prefab.meta" -Pattern '^guid:').Line.Split(':')[1].Trim()
rg -n "$uiGuid|$pinballGuid" Assets ProjectSettings -g "*.unity" -g "*.prefab" -g "*.asset"
```

Expected: no file outside the two deletion targets references either GUID. If a retained asset references one, stop this task and report the exact reference instead of deleting.

- [ ] **Step 4: Delete exactly four approved files with apply_patch**

Do not use a wildcard or recursive deletion. Re-run `Get-ChildItem Assets/04. Prefabs` and confirm Ball, SmallPin, and BigBumper remain.

- [ ] **Step 5: Run asset tests and verify pass**

Expected: all obsolete/remain tests pass.

- [ ] **Step 6: Commit the deletion**

```powershell
git add -- "Assets/04. Prefabs/UI.prefab" "Assets/04. Prefabs/UI.prefab.meta" "Assets/04. Prefabs/Pinball.prefab" "Assets/04. Prefabs/Pinball.prefab.meta" "Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs"
git commit -m "chore: remove obsolete aggregate prefabs"
```

### Task 4: 씬·프리팹 연결을 자동 검증

**Files:**
- Modify: `Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs`

**Interfaces:**
- Verifies plan 03 Game scene and AllyUnit prefab wiring.

- [ ] **Step 1: Add scene and prefab wiring tests**

```csharp
using UnityEditor.SceneManagement;

[Test]
public void AllyPrefabHasDragCollider()
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/04. Prefabs/AllyUnit.prefab");
    Assert.That(prefab.GetComponent<BoxCollider2D>(), Is.Not.Null);
    Assert.That(prefab.GetComponent<AllyDragController>(), Is.Not.Null);
}

[Test]
public void GameSceneContainsPreplacedModalPanels()
{
    var scene = EditorSceneManager.OpenScene(
        "Assets/01. Scenes/02. Game.unity",
        OpenSceneMode.Single);
    Assert.That(scene.IsValid(), Is.True);
    Assert.That(Object.FindFirstObjectByType<PromotionPanel>(
        FindObjectsInactive.Include), Is.Not.Null);
    Assert.That(Object.FindFirstObjectByType<BattleResultPanel>(
        FindObjectsInactive.Include), Is.Not.Null);
}
```

- [ ] **Step 2: Run ProjectConfigurationTests**

Expected: prefab and both inactive scene panel tests pass without missing-reference logs.

- [ ] **Step 3: Commit the configuration tests**

```powershell
git add -- "Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs"
git commit -m "test: verify game scene battle wiring"
```

### Task 5: 전체 Unity 및 WebGL 검증

**Files:**
- Create temporarily: `Assets/02. Scripts/Editor/WebGlBuildVerification.cs`
- Delete after execution: build verification script and generated `.meta`
- Output only: `Temp/CodexWebGLBuild`, test XML, Unity logs

**Interfaces:**
- Verifies all production and test assemblies, scenes, and PC WebGL target.

- [ ] **Step 1: Run all EditMode tests from a fresh Unity process**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults "Temp/final-editmode-results.xml" -logFile "Temp/final-editmode.log"
```

Expected: exit code 0 and every test case reports Passed.

- [ ] **Step 2: Import the Game scene and inspect high-signal errors**

```powershell
& $unityEditor -batchmode -nographics -projectPath "$PWD" -scene "Assets/01. Scenes/02. Game.unity" -quit -logFile "Temp/final-game-scene.log"
rg -n "error CS|MissingReferenceException|NullReferenceException|managed reference|Failed to resolve package" "Temp/final-game-scene.log"
```

Expected: no matches.

- [ ] **Step 3: Create an exact temporary WebGL build entry point**

```csharp
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGlBuildVerification
{
    public static void Build()
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray(),
            locationPathName = "Temp/CodexWebGLBuild",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.InvalidOperationException(
                $"WebGL build failed: {report.summary.result}");
        }
    }
}
```

- [ ] **Step 4: Run the WebGL build and remove the temporary entry point**

```powershell
& $unityEditor -batchmode -nographics -projectPath "$PWD" -buildTarget WebGL -executeMethod WebGlBuildVerification.Build -quit -logFile "Temp/final-webgl-build.log"
```

Expected: exit code 0 and `Temp/CodexWebGLBuild` contains a completed build. Delete the temporary script and meta with `apply_patch`, then run Unity compilation once more.

- [ ] **Step 5: Run source and asset audits**

```powershell
rg -n "StartCoroutine|StopCoroutine|IEnumerator|yield return|WaitForSeconds" "Assets/02. Scripts" -g "*.cs"
rg -n "FindSkill|case \"(shield_judgment|blood_whirlwind|wolf_sprint|king_slam)\"|UnitId == \"" "Assets/02. Scripts/Battle" -g "*.cs"
Get-ChildItem -LiteralPath "Assets/04. Prefabs" -File | Select-Object -ExpandProperty Name
git diff --check
```

Expected: the two `rg` commands return no matches; retained prefab list includes AllyUnit, EnemyUnit, Ball, SmallPin, and BigBumper; `git diff --check` returns no errors.

### Task 6: AI 활용 기록과 최종 작업 경계 기록

**Files:**
- Create or append: `.github/ai-usage-log.md`

**Interfaces:**
- Records facts required by repository instructions.

- [ ] **Step 1: Record the completed work with actual evidence**

Add a dated section containing:

- 사용 도구/모델: Codex, 현재 표시 모델명
- 사용자 요청: dev 재분석, 데이터 결합 해소, 잔존 문제 해결, UniRx/UniTask, 수동 합성·5레벨 승급
- AI 제안: Adapter 경계의 노드 그래프, Pending 단일 준비 단계, 사전 배치 모달 UI
- AI 수정 영역: 실제 `git diff --name-status` 결과
- 사용자 결정 영역: 패배 후 Pending, 웨이브 테이블 제외, Title 직접 구현, 아이콘 제외, 프리팹 삭제, URP 17.0.4, 노드 방식
- 중요 지시: `ItemManager` 예외, 코루틴 전부 UniTask, 같은 직업·레벨 드래그 합성
- 테스트: 실제 테스트 수, Unity exit code, WebGL 결과와 로그 경로

Do not claim a build or test that did not run successfully.

- [ ] **Step 2: Commit the factual log**

```powershell
git add -- ".github/ai-usage-log.md"
git commit -m "docs: record AI-assisted battle stabilization"
```

- [ ] **Step 3: Confirm final repository status**

```powershell
git status --short
git log --oneline --max-count=15
```

Expected: clean working tree and a reviewable sequence of focused commits.

- [ ] **Step 4: Prepare the user-facing completion report**

List every solved problem, changed/deleted file group, test and build result, manual verification steps, excluded scope, and the known Title auto-transition limitation. Include no success claim without a corresponding command result or direct inspection.
