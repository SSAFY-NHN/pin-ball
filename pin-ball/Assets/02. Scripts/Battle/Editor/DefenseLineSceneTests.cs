#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DefenseLineSceneTests
{
    [Test]
    public void GameScene_WiresOneDefenseLinePerTeam()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
        DefenseLineTrigger[] lines = Object.FindObjectsByType<DefenseLineTrigger>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(lines.Count(line =>
            line.DefenseTeam == EBattleTeam.Ally), Is.EqualTo(1));
        Assert.That(lines.Count(line =>
            line.DefenseTeam == EBattleTeam.Enemy), Is.EqualTo(1));
        foreach (DefenseLineTrigger line in lines)
        {
            AssertReference(line, "bodyRenderer");
            AssertReference(line, "healthFill");
            Rigidbody2D body = line.GetComponent<Rigidbody2D>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(body.simulated, Is.True);
            Assert.That(body.gravityScale, Is.Zero);
        }

        UnitManager manager = Object.FindFirstObjectByType<UnitManager>();
        AssertReference(manager, "allyDefenseLine");
        AssertReference(manager, "enemyDefenseLine");
    }

    private static void AssertReference(Object target, string propertyName)
    {
        SerializedProperty property =
            new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
    }
}
#endif
