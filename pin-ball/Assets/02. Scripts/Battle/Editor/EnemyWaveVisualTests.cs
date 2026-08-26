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
            Wave(("goblin", 3)),
            Wave(("goblin", 4)),
            Wave(("goblin", 3), ("goblin_archer", 1)),
            Wave(("goblin", 3), ("goblin_archer", 2)),
            Wave(("goblin", 2), ("goblin_archer", 2), ("shield_guard", 1)),
            Wave(("goblin", 2), ("goblin_archer", 1), ("shield_guard", 2)),
            Wave(("goblin", 2), ("goblin_archer", 1), ("shield_guard", 1), ("orc_warrior", 1)),
            Wave(("goblin", 1), ("goblin_archer", 2), ("shield_guard", 1), ("orc_warrior", 1), ("assassin", 1)),
            Wave(("goblin", 1), ("goblin_archer", 1), ("shield_guard", 1), ("orc_warrior", 2)),
            Wave(("goblin", 1), ("goblin_archer", 1), ("shield_guard", 1), ("goblin_king", 1))
        };

        for (var waveIndex = 0; waveIndex < expectedWaves.Length; waveIndex++)
        {
            var actual = collection.waves[waveIndex].Enemies.ToDictionary(
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
            ["goblin"] = "Assets/03. Images/Humans/Patrolman/H_Patrolman.png",
            ["wolf"] = "Assets/03. Images/Humans/Rogue/H_Rogue.png",
            ["goblin_archer"] = "Assets/03. Images/Humans/Archer/H_Archer.png",
            ["goblin_archer_undead"] = "Assets/03. Images/Humans/Archer/H_Archer_EvoSkeleton.png",
            ["shield_guard"] = "Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior.png",
            ["shield_guard_undead"] = "Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior_Undead.png",
            ["orc_warrior"] = "Assets/03. Images/Humans/Knights/H_Warrior.png",
            ["orc_warrior_undead"] = "Assets/03. Images/Humans/Knights/H_Warrior_Undead.png",
            ["assassin"] = "Assets/03. Images/Humans/Rogue/H_Rogue.png",
            ["assassin_undead"] = "Assets/03. Images/Humans/Rogue/H_Rogue_EvoSkeleton.png",
            ["goblin_king"] = "Assets/03. Images/Humans/Boss/H_BoneStalkerBoss.png"
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

        var bossProfile = FindProfile(profiles, "goblin_king");
        var skillFrames = bossProfile.FindPropertyRelative("skillFrames");
        Assert.That(skillFrames.arraySize, Is.GreaterThanOrEqualTo(2));
        Assert.That(
            AssetDatabase.GetAssetPath(
                skillFrames.GetArrayElementAtIndex(0).objectReferenceValue),
            Is.EqualTo("Assets/03. Images/Humans/Boss/H_BoneStalkerBoss_skill.png"));
    }

    [TestCase("goblin_archer", 5, "goblin_archer")]
    [TestCase("goblin_archer", 6, "goblin_archer_undead")]
    [TestCase("shield_guard", 6, "shield_guard_undead")]
    [TestCase("orc_warrior", 6, "orc_warrior_undead")]
    [TestCase("assassin", 6, "assassin_undead")]
    [TestCase("goblin_king", 6, "goblin_king")]
    public void EnemyVisualProfile_ChangesAfterWaveFive(
        string unitId,
        int waveNumber,
        string expectedProfileId)
    {
        var resolver = typeof(EnemyUnit).GetMethod(
            "ResolveVisualProfileId");
        Assert.That(resolver, Is.Not.Null);
        Assert.That(
            resolver.Invoke(null, new object[] { unitId, waveNumber }),
            Is.EqualTo(expectedProfileId));
    }

    [Test]
    public void EnemyBossSkillFeedback_StartsSkillAnimation()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        var instance = Object.Instantiate(prefab);
        try
        {
            var visual = instance.GetComponent<BattleUnitVisual>();
            var renderer = instance.GetComponent<SpriteRenderer>();
            var playSkill = typeof(BattleUnitVisual).GetMethod(
                "PlaySkillAnimation");
            Assert.That(playSkill, Is.Not.Null);

            visual.SetUnitId("goblin_king");
            playSkill.Invoke(visual, null);

            Assert.That(
                AssetDatabase.GetAssetPath(renderer.sprite),
                Is.EqualTo(
                    "Assets/03. Images/Humans/Boss/H_BoneStalkerBoss_skill.png"));
        }
        finally
        {
            Object.DestroyImmediate(instance);
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
