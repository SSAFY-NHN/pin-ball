#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManagerTests
{
    private const string DeveloperScenePath =
        "Assets/01. Scenes/00. Developer.unity";

    [Test]
    public void DeveloperScene_RegistersStartupBgmAndEverySfxClip()
    {
        Scene scene = EditorSceneManager.OpenScene(
            DeveloperScenePath,
            OpenSceneMode.Additive);

        try
        {
            SoundManager manager = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<SoundManager>(true))
                .SingleOrDefault();
            Assert.That(manager, Is.Not.Null);

            var serializedManager = new SerializedObject(manager);
            Assert.That(
                serializedManager.FindProperty("startupBgmName").stringValue,
                Is.EqualTo(SoundName.MainBgm));

            Dictionary<string, string> bgm = ReadClipPaths(
                serializedManager.FindProperty("_bgmClips"));
            Assert.That(bgm, Has.Count.EqualTo(1));
            Assert.That(
                bgm[SoundName.MainBgm],
                Is.EqualTo("Assets/06. Sounds/BGM/main2.mp3"));

            Dictionary<string, string> sfx = ReadClipPaths(
                serializedManager.FindProperty("_sfxClips"));
            var expectedSfx = new Dictionary<string, string>
            {
                [SoundName.Arrow] = "arrow_sound.mp3",
                [SoundName.BallHit] = "ball_hit_sound_lastpart_use.mp3",
                [SoundName.BumperHit] = "bumper_hit_sound.mp3",
                [SoundName.BuyItem] = "Buy_item.mp3",
                [SoundName.ClassicPunch] = "classic-punch.mp3",
                [SoundName.Evolution] = "evolution_sound.mp3",
                [SoundName.EnemyHealing] = "healing2_sound.mp3",
                [SoundName.AllyHealing] = "healing_sound.mp3",
                [SoundName.Hit] = "hit_sound.mp3",
                [SoundName.MagicSpell] = "magicspell_sound.mp3",
                [SoundName.SmallPinHit] = "pinball_pin_small_hit_sound.mp3",
                [SoundName.Spring] = "spring_sound.mp3",
                [SoundName.SpringPull] = "spring_sound2.mp3",
                [SoundName.UnitSpawn] = "unit_spawn.mp3",
                [SoundName.WaveFailed] = "wave_faild.mp3",
                [SoundName.WaveStart] = "wave_start.mp3",
                [SoundName.WaveWin] = "Wave_Win.mp3"
            };

            Assert.That(sfx, Has.Count.EqualTo(expectedSfx.Count));
            foreach (var expected in expectedSfx)
            {
                Assert.That(sfx, Does.ContainKey(expected.Key));
                Assert.That(
                    sfx[expected.Key],
                    Is.EqualTo($"Assets/06. Sounds/SFX/{expected.Value}"));
            }

            var bgmPlayer = serializedManager
                .FindProperty("_bgmPlayer")
                .objectReferenceValue as AudioSource;
            Assert.That(bgmPlayer, Is.Not.Null);
            Assert.That(bgmPlayer.loop, Is.True);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [TestCase("archer", SoundName.Arrow)]
    [TestCase("ranger", SoundName.Arrow)]
    [TestCase("mage", SoundName.MagicSpell)]
    [TestCase("frost", SoundName.MagicSpell)]
    [TestCase("pyromancer", SoundName.MagicSpell)]
    [TestCase("warrior", SoundName.ClassicPunch)]
    [TestCase("knight", SoundName.ClassicPunch)]
    [TestCase("goblin", SoundName.ClassicPunch)]
    public void GetAttack_ReturnsSoundForUnitRole(
        string unitId,
        string expectedSound)
    {
        Assert.That(SoundName.GetAttack(unitId), Is.EqualTo(expectedSound));
    }

    private static Dictionary<string, string> ReadClipPaths(
        SerializedProperty clips)
    {
        Assert.That(clips, Is.Not.Null);
        var result = new Dictionary<string, string>();

        for (var index = 0; index < clips.arraySize; index++)
        {
            SerializedProperty sound = clips.GetArrayElementAtIndex(index);
            string name = sound.FindPropertyRelative("Name").stringValue;
            Object clip = sound.FindPropertyRelative("Clip").objectReferenceValue;
            Assert.That(clip, Is.Not.Null, $"Missing audio clip: {name}");
            result.Add(name, AssetDatabase.GetAssetPath(clip));
        }

        return result;
    }
}
#endif
