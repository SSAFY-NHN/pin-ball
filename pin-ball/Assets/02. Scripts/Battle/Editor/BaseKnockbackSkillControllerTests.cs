#if UNITY_EDITOR
using NUnit.Framework;

public sealed class BaseKnockbackSkillControllerTests
{
    [Test]
    public void StartWave_ResetsLockedStateAndElapsedTime()
    {
        var controller = new BaseKnockbackSkillController();
        controller.Advance(30f, true);
        controller.TryConfirmUse(true);

        controller.StartWave();

        Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Locked));
        Assert.That(controller.ElapsedTime, Is.Zero);
        Assert.That(controller.RemainingTime, Is.EqualTo(30f));
        Assert.That(controller.CanUse, Is.False);
    }

    [Test]
    public void Advance_UnlocksAtThirtyGameSeconds()
    {
        var controller = new BaseKnockbackSkillController();

        controller.Advance(29.99f, true);
        Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Locked));

        controller.Advance(0.01f, true);
        Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Ready));
        Assert.That(controller.ElapsedTime, Is.EqualTo(30f).Within(0.0001f));
        Assert.That(controller.CanUse, Is.True);
    }

    [Test]
    public void Advance_UsesSuppliedGameTimeAndIgnoresInactiveOrNegativeTime()
    {
        var controller = new BaseKnockbackSkillController();

        controller.Advance(30f, false);
        controller.Advance(-5f, true);
        Assert.That(controller.ElapsedTime, Is.Zero);

        for (var i = 0; i < 15; i++) controller.Advance(2f, true);

        Assert.That(controller.ElapsedTime, Is.EqualTo(30f));
        Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Ready));
    }

    [Test]
    public void TryConfirmUse_ConsumesOnlyAReadySuccessfulUse()
    {
        var controller = new BaseKnockbackSkillController();

        Assert.That(controller.TryConfirmUse(true), Is.False);
        controller.Advance(30f, true);
        Assert.That(controller.TryConfirmUse(false), Is.False);
        Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Ready));
        Assert.That(controller.TryConfirmUse(true), Is.True);
        Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Used));
        Assert.That(controller.TryConfirmUse(true), Is.False);
    }

    [Test]
    public void Advance_ReportsOnlyDisplayedSecondOrStateChanges()
    {
        var controller = new BaseKnockbackSkillController();

        Assert.That(controller.Advance(0.1f, true), Is.False);
        Assert.That(controller.Advance(0.1f, true), Is.False);
        Assert.That(controller.Advance(28.8f, true), Is.True);
        Assert.That(controller.Advance(0.9f, true), Is.False);
        Assert.That(controller.Advance(0.1f, true), Is.True);
        Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Ready));
    }
}
#endif
