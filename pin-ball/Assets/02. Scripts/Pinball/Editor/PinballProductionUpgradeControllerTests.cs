using NUnit.Framework;

public sealed class PinballProductionUpgradeControllerTests
{
    [Test]
    public void GetNextCost_UsesCeiledExponentialFormula()
    {
        var controller = CreateController();

        Assert.That(controller.GetNextCost(EPinballProductionUpgrade.BumperIncome), Is.EqualTo(25));
        Assert.That(controller.TryPurchase(EPinballProductionUpgrade.BumperIncome, 25), Is.True);
        Assert.That(controller.GetNextCost(EPinballProductionUpgrade.BumperIncome), Is.EqualTo(40));
    }

    [Test]
    public void BumperIncome_IncreasesLinearlyAfterPurchase()
    {
        var controller = CreateController();

        Assert.That(controller.BumperIncome, Is.EqualTo(1));
        Assert.That(controller.TryPurchase(EPinballProductionUpgrade.BumperIncome, 25), Is.True);
        Assert.That(controller.BumperIncome, Is.EqualTo(2));
    }

    [Test]
    public void TryPurchase_RejectsInsufficientGoldWithoutMutation()
    {
        var controller = CreateController();

        Assert.That(controller.TryPurchase(EPinballProductionUpgrade.AddBall, 149), Is.False);
        Assert.That(controller.GetLevel(EPinballProductionUpgrade.AddBall), Is.Zero);
        Assert.That(controller.PermanentBallCount, Is.EqualTo(1));
    }

    [Test]
    public void AddBall_StopsAtTenPermanentBalls()
    {
        var controller = CreateController();

        for (var i = 0; i < 9; i++)
        {
            Assert.That(controller.TryPurchase(EPinballProductionUpgrade.AddBall, int.MaxValue), Is.True);
        }

        Assert.That(controller.PermanentBallCount, Is.EqualTo(10));
        Assert.That(controller.TryPurchase(EPinballProductionUpgrade.AddBall, int.MaxValue), Is.False);
    }

    [Test]
    public void RespawnDelay_StopsAtMinimumDelay()
    {
        var controller = CreateController();

        for (var i = 0; i < 20; i++)
        {
            controller.TryPurchase(EPinballProductionUpgrade.SupplySpeed, int.MaxValue);
        }

        Assert.That(controller.RespawnDelay, Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(controller.TryPurchase(EPinballProductionUpgrade.SupplySpeed, int.MaxValue), Is.False);
    }

    [Test]
    public void ResetForNewRun_RestoresInitialProductionState()
    {
        var controller = CreateController();
        controller.TryPurchase(EPinballProductionUpgrade.BumperIncome, int.MaxValue);
        controller.TryPurchase(EPinballProductionUpgrade.AddBall, int.MaxValue);
        controller.TryPurchase(EPinballProductionUpgrade.SupplySpeed, int.MaxValue);

        controller.ResetForNewRun();

        Assert.That(controller.BumperIncome, Is.EqualTo(1));
        Assert.That(controller.PermanentBallCount, Is.EqualTo(1));
        Assert.That(controller.RespawnDelay, Is.EqualTo(3f));
    }

    private static PinballProductionUpgradeController CreateController()
    {
        return new PinballProductionUpgradeController(
            new PinballProductionUpgradeSettings(1, 1, 25, 1.6f, 20),
            new PinballProductionUpgradeSettings(1, 1, 150, 2f, 9),
            new PinballProductionUpgradeSettings(3f, -0.25f, 50, 1.7f, 9),
            0.75f);
    }
}
