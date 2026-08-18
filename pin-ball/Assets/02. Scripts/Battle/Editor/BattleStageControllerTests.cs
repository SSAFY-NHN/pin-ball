#if UNITY_EDITOR
using NUnit.Framework;

public sealed class BattleStageControllerTests
{
    [Test]
    public void TryScheduleNextStage_KeepsBattleActiveAndAdvancesOnce()
    {
        var controller = new BattleStageController();
        Assert.That(controller.TryStart(), Is.True);

        Assert.That(controller.TryScheduleNextStage(10f, 2f), Is.True);
        Assert.That(controller.State, Is.EqualTo(EWaveState.Active));
        Assert.That(controller.CurrentStage, Is.EqualTo(2));
        Assert.That(controller.TryScheduleNextStage(10f, 2f), Is.False);
        Assert.That(controller.CurrentStage, Is.EqualTo(2));
    }

    [Test]
    public void TryCompleteNextStageSchedule_OnlyCompletesAfterDeadlineOnce()
    {
        var controller = new BattleStageController();
        controller.TryStart();
        controller.TryScheduleNextStage(10f, 2f);

        Assert.That(controller.TryCompleteNextStageSchedule(11.99f), Is.False);
        Assert.That(controller.TryCompleteNextStageSchedule(12f), Is.True);
        Assert.That(controller.TryCompleteNextStageSchedule(12f), Is.False);
    }

    [Test]
    public void Recovery_KeepsCurrentStageAndReturnsToActiveAfterDeadline()
    {
        var controller = new BattleStageController();
        controller.TryStart();

        Assert.That(controller.TryBeginRecovery(20f, 3f), Is.True);
        Assert.That(controller.State, Is.EqualTo(EWaveState.Recovering));
        Assert.That(controller.CurrentStage, Is.EqualTo(1));
        Assert.That(controller.TryCompleteRecovery(22.99f), Is.False);
        Assert.That(controller.TryCompleteRecovery(23f), Is.True);
        Assert.That(controller.State, Is.EqualTo(EWaveState.Active));
        Assert.That(controller.CurrentStage, Is.EqualTo(1));
    }
}
#endif
