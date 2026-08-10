#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BattleUnitVisualTests
{
    private const string AllyPrefabPath =
        "Assets/04. Prefabs/AllyUnit.prefab";

    [TestCase(
        "warrior",
        "Dog1_Farmer_Sword_Tiny32",
        "Dog1_Farmer_Sword_Tiny32_Walk_",
        "Dog1_Farmer_Sword_Tiny32_Attack_",
        12f,
        10f)]
    [TestCase(
        "archer",
        "Cat1_archer_Tiny32",
        "Cat1_archer_Tiny32_Walk_",
        "Cat1_archer_Tiny32_Attack_",
        12f,
        12f)]
    [TestCase(
        "mage",
        "Rabbit1_Mage_Tiny32",
        "Rabbit1_Mage_Walk_",
        "Rabbit_mage_Attack_",
        12f,
        10f)]
    [TestCase(
        "lancer",
        "Bear1_Fighter_Tiny32",
        "Bear1_Fighter_Walk_",
        "Bear1_Fighter_Attack_",
        12f,
        12f)]
    public void AllyPrefab_UnitProfileUsesExpectedMotionFrames(
        string unitId,
        string idlePrefix,
        string movePrefix,
        string attackPrefix,
        float moveFramesPerSecond,
        float attackFramesPerSecond)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AllyPrefabPath);
        Assert.That(prefab, Is.Not.Null);

        var visual = prefab.GetComponent<BattleUnitVisual>();
        Assert.That(visual, Is.Not.Null);

        var serializedVisual = new SerializedObject(visual);
        var profiles = serializedVisual.FindProperty("unitAnimations");
        Assert.That(profiles, Is.Not.Null);

        SerializedProperty unitProfile = null;
        for (var index = 0; index < profiles.arraySize; index++)
        {
            var candidate = profiles.GetArrayElementAtIndex(index);
            if (candidate.FindPropertyRelative("unitId").stringValue != unitId)
            {
                continue;
            }

            unitProfile = candidate;
            break;
        }

        Assert.That(unitProfile, Is.Not.Null);
        var idleFrames = unitProfile.FindPropertyRelative("idleFrames");
        var moveFrames = unitProfile.FindPropertyRelative("moveFrames");
        var attackFrames = unitProfile.FindPropertyRelative("attackFrames");

        Assert.That(
            idleFrames.arraySize,
            Is.GreaterThanOrEqualTo(1));
        Assert.That(
            moveFrames.arraySize,
            Is.GreaterThanOrEqualTo(2));
        Assert.That(
            attackFrames.arraySize,
            Is.GreaterThanOrEqualTo(2));
        Assert.That(
            idleFrames.GetArrayElementAtIndex(0).objectReferenceValue.name,
            Does.StartWith(idlePrefix));
        Assert.That(
            moveFrames.GetArrayElementAtIndex(0).objectReferenceValue.name,
            Does.StartWith(movePrefix));
        Assert.That(
            attackFrames.GetArrayElementAtIndex(0).objectReferenceValue.name,
            Does.StartWith(attackPrefix));
        Assert.That(
            unitProfile.FindPropertyRelative("moveFramesPerSecond").floatValue,
            Is.EqualTo(moveFramesPerSecond));
        Assert.That(
            unitProfile.FindPropertyRelative("attackFramesPerSecond").floatValue,
            Is.EqualTo(attackFramesPerSecond));
    }

    [TestCase(
        "Assets/05. Animations/Rabbit/Rabbit1_Mage_Walk.anim",
        "Rabbit1_Mage_Walk_",
        9,
        true)]
    [TestCase(
        "Assets/05. Animations/Rabbit/Rabbit1_Mage_Attack.anim",
        "Rabbit_mage_Attack_",
        8,
        false)]
    [TestCase(
        "Assets/05. Animations/Bear/Bear1_Fighter_Walk.anim",
        "Bear1_Fighter_Walk_",
        10,
        true)]
    [TestCase(
        "Assets/05. Animations/Bear/Bear1_Fighter_Attack.anim",
        "Bear1_Fighter_Attack_",
        13,
        false)]
    public void AnimalClip_UsesCurrentSpriteAssets(
        string clipPath,
        string spritePrefix,
        int expectedFrameCount,
        bool expectedLoop)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        Assert.That(clip, Is.Not.Null);

        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        Assert.That(bindings, Has.Length.EqualTo(1));

        var frames = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
        Assert.That(frames, Has.Length.EqualTo(expectedFrameCount));
        foreach (var frame in frames)
        {
            Assert.That(frame.value, Is.TypeOf<Sprite>());
            Assert.That(frame.value.name, Does.StartWith(spritePrefix));
        }

        Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime,
            Is.EqualTo(expectedLoop));
    }
}
#endif
