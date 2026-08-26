#if UNITY_EDITOR
using System;
using System.Reflection;

using NUnit.Framework;
using UnityEngine;

public sealed class PreparationCountdownBattleManagerTests
{
    private GameObject gameObject;

    [TearDown]
    public void TearDown()
    {
        if (gameObject != null) UnityEngine.Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void AdvancePreparationCountdown_PendingExpirationStartsWaveOnce()
    {
        BattleManager manager = CreateManager(EWaveState.Pending);
        int startAttempts = 0;

        manager.AdvancePreparationCountdown(60f, () =>
        {
            startAttempts++;
            return true;
        });
        manager.AdvancePreparationCountdown(1f, () =>
        {
            startAttempts++;
            return true;
        });

        Assert.That(manager.PreparationRemainingTime, Is.Zero);
        Assert.That(startAttempts, Is.EqualTo(1));
    }

    [Test]
    public void AdvancePreparationCountdown_ActiveStateDoesNotConsumeTime()
    {
        BattleManager manager = CreateManager(EWaveState.Active);

        manager.AdvancePreparationCountdown(30f, () => true);

        Assert.That(manager.PreparationRemainingTime, Is.EqualTo(60f));
    }

    [Test]
    public void AdvancePreparationCountdown_ActiveTutorialPausesTime()
    {
        BattleManager manager = CreateManager(EWaveState.Pending);
        var overlay = new GameObject("TutorialOverlay");
        overlay.transform.SetParent(gameObject.transform);
        SetField(manager, "tutorialOverlay", overlay);

        manager.AdvancePreparationCountdown(30f, () => true);

        Assert.That(manager.PreparationRemainingTime, Is.EqualTo(60f));
    }

    private BattleManager CreateManager(EWaveState state)
    {
        gameObject = new GameObject("Battle Manager");
        var manager = gameObject.AddComponent<BattleManager>();
        var runState = new BattleRunState(
            new[] { new BattleWaveData() },
            true,
            3);
        runState.ChangeState(state);
        SetField(manager, "runState", runState);
        SetField(manager, "preparationCountdown", new PreparationCountdown(60f));
        SetField(manager, "<IsInitialized>k__BackingField", true);
        return manager;
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
    }
}
#endif
