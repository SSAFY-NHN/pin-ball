#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class AllyInteractionPolicyTests
{
    [TestCase(false, false)]
    [TestCase(true, true)]
    public void DeployPurchasedAlly_ShowsImmediatelyAndUsesBattleState(
        bool battleActive,
        bool expectedBattleActive)
    {
        var allyObject = new GameObject("ally");
        try
        {
            var ally = allyObject.AddComponent<AllyUnit>();
            ally.Initialize(new BattleUnitStats { MaxHp = 10f }, null);
            allyObject.SetActive(false);

            UnitManager.DeployPurchasedAlly(ally, battleActive);

            Assert.That(allyObject.activeSelf, Is.True);
            Assert.That(ally.IsBattleActive, Is.EqualTo(expectedBattleActive));
        }
        finally
        {
            Object.DestroyImmediate(allyObject);
        }
    }

    [Test]
    public void DragDrop_DoesNotMergeAllies()
    {
        Assert.That(UnitManager.ShouldAttemptAllyMergeOnDrop(), Is.False);
    }
}
#endif
