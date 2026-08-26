#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EnemyWaveVisualTests
{
    private const string EnemyPrefabPath =
        "Assets/04. Prefabs/EnemyUnit.prefab";
    private const string AllyPrefabPath =
        "Assets/04. Prefabs/AllyUnit.prefab";

    [Test]
    public void BattleWaves_UseApprovedEnemyProgression()
    {
        var textAsset = Resources.Load<TextAsset>("Data/BattleWaveData");
        Assert.That(textAsset, Is.Not.Null);

        var collection =
            JsonUtility.FromJson<BattleWaveDataCollection>(textAsset.text);
        Assert.That(collection?.waves, Has.Length.EqualTo(10));

        var expectedWaves = new[]
        {
            Wave(("PatrolMan", 3)),
            Wave(("PatrolMan", 3), ("Rogue", 1)),
            Wave(("PatrolMan", 2), ("Rogue", 1), ("Archer", 1)),
            Wave(("PatrolMan", 2), ("Rogue", 1), ("Archer", 2)),
            Wave(("PatrolMan", 1), ("Rogue", 2), ("Archer", 1), ("MaceWarrior", 1)),
            Wave(("PatrolMan", 1), ("Rogue", 1), ("Archer", 1), ("MaceWarrior", 2)),
            Wave(("Rogue", 1), ("Archer", 1), ("MaceWarrior", 1), ("Knights", 1)),
            Wave(("Rogue", 1), ("Archer", 2), ("MaceWarrior", 1), ("Knights", 1)),
            Wave(("Rogue", 1), ("Archer", 1), ("MaceWarrior", 1), ("Knights", 2)),
            Wave(("Archer", 1), ("MaceWarrior", 1), ("Knights", 1), ("DarkMageBoss", 1))
        };

        for (var waveIndex = 0; waveIndex < expectedWaves.Length; waveIndex++)
        {
            var actual = collection.waves[waveIndex].Enemies
                .Select(enemy => $"{enemy.EnemyId}:{enemy.Count}")
                .ToArray();
            Assert.That(
                actual,
                Is.EqualTo(expectedWaves[waveIndex]),
                $"Wave {waveIndex + 1}");
        }

        Assert.That(
            collection.waves.Select(wave => wave.IsElite),
            Is.EqualTo(new[]
            {
                false, false, false, false, true,
                false, false, false, true, false
            }));
        Assert.That(
            collection.waves.Select(wave => wave.IsBoss),
            Is.EqualTo(new[]
            {
                false, false, false, false, false,
                false, false, false, false, true
            }));
    }

    [Test]
    public void EnemyData_UsesApprovedMeleeProgression()
    {
        var textAsset = Resources.Load<TextAsset>("Data/EnemyUnitData");
        Assert.That(textAsset, Is.Not.Null);

        var collection =
            JsonUtility.FromJson<EnemyUnitDataCollection>(textAsset.text);
        var enemies = collection.units.ToDictionary(enemy => enemy.id);

        Assert.That(enemies.Keys, Does.Contain("PatrolMan"));
        Assert.That(enemies.Keys, Does.Contain("Rogue"));
        Assert.That(enemies.Keys, Does.Contain("Archer"));
        Assert.That(enemies.Keys, Does.Contain("MaceWarrior"));
        Assert.That(enemies.Keys, Does.Contain("Knights"));
        Assert.That(enemies.Keys, Does.Contain("DarkMageBoss"));
        Assert.That(enemies.Keys, Does.Not.Contain("goblin"));
        Assert.That(enemies.Keys, Does.Not.Contain("assassin"));
        Assert.That(enemies.Keys, Does.Not.Contain("goblin_archer"));
        Assert.That(enemies.Keys, Does.Not.Contain("shield_guard"));
        Assert.That(enemies.Keys, Does.Not.Contain("orc_warrior"));
        Assert.That(enemies.Keys, Does.Not.Contain("goblin_king"));

        var patrolMan = enemies["PatrolMan"];
        var rogue = enemies["Rogue"];
        var maceWarrior = enemies["MaceWarrior"];
        var knights = enemies["Knights"];
        Assert.That(rogue.health, Is.EqualTo(120));
        Assert.That(rogue.attack, Is.EqualTo(16));
        Assert.That(rogue.defense, Is.EqualTo(4));
        Assert.That(rogue.moveSpeed, Is.EqualTo(1.6f));
        Assert.That(rogue.attackSpeed, Is.EqualTo(0.9f));
        Assert.That(rogue.attackRange, Is.EqualTo(1.1f));
        Assert.That(rogue.breachDamage, Is.EqualTo(2));
        Assert.That(rogue.Skills, Is.Empty);
        Assert.That(maceWarrior.attackSpeed, Is.EqualTo(0.7f));
        Assert.That(
            new[]
            {
                patrolMan.attack,
                rogue.attack,
                maceWarrior.attack,
                knights.attack
            },
            Is.EqualTo(new[] { 12, 16, 18, 22 }));
    }

    [Test]
    public void EnemyPrefab_HasExpectedUnitAnimationProfiles()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        Assert.That(prefab, Is.Not.Null);

        var visual = prefab.GetComponent<BattleUnitVisual>();
        Assert.That(visual, Is.Not.Null);

        var expectedIdlePaths = new Dictionary<string, string>
        {
            ["PatrolMan"] = "Assets/03. Images/Humans/Patrolman/H_Patrolman.png",
            ["Rogue"] = "Assets/03. Images/Humans/Rogue/H_Rogue.png",
            ["wolf"] = "Assets/03. Images/Humans/Rogue/H_Rogue.png",
            ["Archer"] = "Assets/03. Images/Humans/Archer/H_Archer.png",
            ["MaceWarrior"] = "Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior.png",
            ["Knights"] = "Assets/03. Images/Humans/Knights/H_Warrior.png",
            ["DarkMageBoss"] = "Assets/03. Images/Humans/Boss/H_MountedMageBoss.png"
        };

        var serializedVisual = new SerializedObject(visual);
        var profiles = serializedVisual.FindProperty("unitAnimations");
        Assert.That(profiles.arraySize, Is.EqualTo(expectedIdlePaths.Count));

        foreach (var expected in expectedIdlePaths)
        {
            var profile = FindProfile(profiles, expected.Key);
            Assert.That(profile, Is.Not.Null, expected.Key);

            var idleFrames = profile.FindPropertyRelative("idleFrames");
            var moveFrames = profile.FindPropertyRelative("moveFrames");
            var attackFrames = profile.FindPropertyRelative("attackFrames");
            Assert.That(idleFrames.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(moveFrames.arraySize, Is.GreaterThanOrEqualTo(2));
            Assert.That(attackFrames.arraySize, Is.GreaterThanOrEqualTo(2));
            AssertFramesAreAssigned(idleFrames, expected.Key, "idle");
            AssertFramesAreAssigned(moveFrames, expected.Key, "move");
            AssertFramesAreAssigned(attackFrames, expected.Key, "attack");
            Assert.That(
                AssetDatabase.GetAssetPath(
                    idleFrames.GetArrayElementAtIndex(0).objectReferenceValue),
                Is.EqualTo(expected.Value));
            Assert.That(
                profile.FindPropertyRelative("sourceFacesRight").boolValue,
                Is.True);
        }
    }

    [Test]
    public void EnemyPrefab_HWarriorMatchesLargestBaseAllyHeight()
    {
        var enemyPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        var allyPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(AllyPrefabPath);
        Assert.That(enemyPrefab, Is.Not.Null);
        Assert.That(allyPrefab, Is.Not.Null);

        var warrior = LoadSprite(
            "Assets/03. Images/Humans/Knights/H_Warrior.png",
            "H_Warrior_0");
        var bear = LoadSprite(
            "Assets/03. Images/Animals/Bear/Bear1_Fighter_Tiny32.png",
            "Bear1_Fighter_Tiny32_0");

        var enemyHeight = warrior.bounds.size.y * enemyPrefab.transform.localScale.y;
        var largestAllyHeight = bear.bounds.size.y * allyPrefab.transform.localScale.y;

        Assert.That(enemyPrefab.transform.localScale.x, Is.EqualTo(1.75f));
        Assert.That(enemyPrefab.transform.localScale.y, Is.EqualTo(1.75f));
        Assert.That(enemyHeight, Is.EqualTo(largestAllyHeight).Within(0.01f));

        var healthBar = enemyPrefab.transform.Find("WorldHealthBar");
        Assert.That(healthBar, Is.Not.Null);
        Assert.That(
            healthBar.localScale.x * enemyPrefab.transform.localScale.x,
            Is.EqualTo(1f).Within(0.001f));
        Assert.That(
            healthBar.localPosition.y * enemyPrefab.transform.localScale.y,
            Is.EqualTo(0.996f).Within(0.001f));
    }

    private static string[] Wave(
        params (string id, int count)[] enemies)
    {
        return enemies
            .Select(enemy => $"{enemy.id}:{enemy.count}")
            .ToArray();
    }

    private static void AssertFramesAreAssigned(
        SerializedProperty frames,
        string unitId,
        string animationName)
    {
        for (var index = 0; index < frames.arraySize; index++)
        {
            Assert.That(
                frames.GetArrayElementAtIndex(index).objectReferenceValue,
                Is.Not.Null,
                $"{unitId} {animationName} frame {index}");
        }
    }

    private static SerializedProperty FindProfile(
        SerializedProperty profiles,
        string unitId)
    {
        for (var index = 0; index < profiles.arraySize; index++)
        {
            var profile = profiles.GetArrayElementAtIndex(index);
            if (profile.FindPropertyRelative("unitId").stringValue == unitId)
            {
                return profile;
            }
        }

        return null;
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }
}
#endif
