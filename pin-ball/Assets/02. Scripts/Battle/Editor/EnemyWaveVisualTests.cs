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
            Wave(("goblin", 4)),
            Wave(("wolf", 3)),
            Wave(("goblin", 3), ("goblin_archer", 1)),
            Wave(("goblin", 4), ("goblin_archer", 1)),
            Wave(("shield_guard", 3)),
            Wave(("shield_guard", 2), ("goblin", 3)),
            Wave(("orc_warrior", 2), ("troll", 1)),
            Wave(("shaman", 2), ("assassin", 2)),
            Wave(("ogre_elite", 1), ("dark_mage_elite", 1), ("shield_guard", 1), ("assassin", 1)),
            Wave(("goblin_king", 1), ("goblin", 3))
        };

        for (var waveIndex = 0; waveIndex < expectedWaves.Length; waveIndex++)
        {
            var actual = collection.waves[waveIndex].InitialAssault.ToDictionary(
                enemy => enemy.EnemyId,
                enemy => enemy.Count);
            Assert.That(
                actual,
                Is.EquivalentTo(expectedWaves[waveIndex]),
                $"Wave {waveIndex + 1}");
        }
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
            ["goblin"] = "Assets/03. Images/Humans/Rogue/H_Rogue.png",
            ["wolf"] = "Assets/03. Images/Humans/Rogue/H_Rogue.png",
            ["goblin_archer"] = "Assets/03. Images/Humans/Archer/H_Archer.png",
            ["shield_guard"] = "Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior.png",
            ["orc_warrior"] = "Assets/03. Images/Humans/Knights/H_Warrior.png",
            ["goblin_king"] = "Assets/03. Images/Humans/Boss/H_MountedMageBoss.png"
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

    private static Dictionary<string, int> Wave(
        params (string id, int count)[] enemies)
    {
        return enemies.ToDictionary(enemy => enemy.id, enemy => enemy.count);
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
