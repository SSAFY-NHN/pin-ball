#if UNITY_EDITOR
using NUnit.Framework;

public sealed class TacticalReinforcementControllerTests
{
    [Test]
    public void ObserveCombo_FirstThresholdReachGrantsTicket()
    {
        var controller = new TacticalReinforcementController(5);

        Assert.That(controller.ObserveCombo(4), Is.False);
        Assert.That(controller.ObserveCombo(5), Is.True);
        Assert.That(controller.HasTicket, Is.True);
    }

    [Test]
    public void ObserveCombo_SameComboSegmentCannotGrantTwiceAfterConsumption()
    {
        var controller = new TacticalReinforcementController(5);
        controller.ObserveCombo(5);
        controller.Consume();

        Assert.That(controller.ObserveCombo(4), Is.False);
        Assert.That(controller.ObserveCombo(6), Is.False);
        Assert.That(controller.HasTicket, Is.False);
    }

    [Test]
    public void ObserveCombo_ResetAllowsGrantInNextComboSegment()
    {
        var controller = new TacticalReinforcementController(5);
        controller.ObserveCombo(5);
        controller.Consume();
        controller.ObserveCombo(0);

        Assert.That(controller.ObserveCombo(5), Is.True);
        Assert.That(controller.HasTicket, Is.True);
    }

    [Test]
    public void GrantFromJackpot_DoesNotStackBeyondOneTicket()
    {
        var controller = new TacticalReinforcementController(5);

        Assert.That(controller.GrantFromJackpot(), Is.True);
        Assert.That(controller.GrantFromJackpot(), Is.False);
        Assert.That(controller.HasTicket, Is.True);
    }

    [Test]
    public void Consume_OnlyChangesStateWhenTicketIsHeld()
    {
        var controller = new TacticalReinforcementController(5);

        Assert.That(controller.Consume(), Is.False);
        controller.GrantFromJackpot();
        Assert.That(controller.Consume(), Is.True);
        Assert.That(controller.HasTicket, Is.False);
    }

    [Test]
    public void TryUse_FailedSpawnKeepsTicket()
    {
        var controller = new TacticalReinforcementController(5);
        controller.GrantFromJackpot();

        Assert.That(controller.TryUse(() => false), Is.False);
        Assert.That(controller.HasTicket, Is.True);
    }

    [Test]
    public void TryUse_SuccessfulSpawnConsumesTicket()
    {
        var controller = new TacticalReinforcementController(5);
        controller.GrantFromJackpot();

        Assert.That(controller.TryUse(() => true), Is.True);
        Assert.That(controller.HasTicket, Is.False);
    }
}
#endif
