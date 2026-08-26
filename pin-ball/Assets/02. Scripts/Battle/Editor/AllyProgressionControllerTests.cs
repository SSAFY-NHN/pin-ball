#if UNITY_EDITOR
using NUnit.Framework;

public sealed class AllyProgressionControllerTests
{
    [Test]
    public void NewController_StartsBaseJobsAtLevelOneWithExpectedCosts()
    {
        var controller = new AllyProgressionController();

        Assert.That(controller.GetLevel("warrior"), Is.EqualTo(1));
        Assert.That(controller.GetNextCost("warrior"), Is.EqualTo(150));
        Assert.That(controller.TryLevelUp(
            "warrior", true, 150, out AllyProgressionResult result), Is.True);
        Assert.That(result.Level, Is.EqualTo(2));
        Assert.That(controller.GetNextCost("warrior"), Is.EqualTo(203));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("knight")]
    [TestCase("missing")]
    public void TryLevelUp_InvalidRootJobDoesNotChangeProgression(string unitId)
    {
        var controller = new AllyProgressionController();

        Assert.That(controller.TryLevelUp(
            unitId, true, int.MaxValue, out _), Is.False);
        Assert.That(controller.GetLevel("warrior"), Is.EqualTo(1));
    }

    [Test]
    public void TryLevelUp_RequiresOwnershipAndEnoughGold()
    {
        var controller = new AllyProgressionController();

        Assert.That(controller.CanLevelUp("warrior", false, 9999), Is.False);
        Assert.That(controller.CanLevelUp("warrior", true, 149), Is.False);
        Assert.That(controller.TryLevelUp(
            "warrior", false, 9999, out _), Is.False);
        Assert.That(controller.TryLevelUp(
            "warrior", true, 149, out _), Is.False);
        Assert.That(controller.GetLevel("warrior"), Is.EqualTo(1));
    }

    [Test]
    public void LevelFiveAndTen_UnlockJobsInConfiguredOrder()
    {
        var controller = new AllyProgressionController();

        AllyProgressionResult result = default;
        while (controller.GetLevel("warrior") < 5)
        {
            Assert.That(controller.TryLevelUp(
                "warrior", true, int.MaxValue, out result), Is.True);
        }

        Assert.That(result.Level, Is.EqualTo(5));
        Assert.That(result.UnlockedUnitId, Is.EqualTo("knight"));
        Assert.That(controller.IsUnlocked("knight"), Is.True);
        Assert.That(controller.IsUnlocked("berserker"), Is.False);

        while (controller.GetLevel("warrior") < 10)
        {
            Assert.That(controller.TryLevelUp(
                "warrior", true, int.MaxValue, out result), Is.True);
        }

        Assert.That(result.Level, Is.EqualTo(10));
        Assert.That(result.UnlockedUnitId, Is.EqualTo("berserker"));
        Assert.That(controller.IsUnlocked("berserker"), Is.True);
        Assert.That(controller.CanLevelUp("warrior", true, int.MaxValue), Is.False);
        Assert.That(controller.TryLevelUp(
            "warrior", true, int.MaxValue, out _), Is.False);
    }

    [Test]
    public void Reset_RestoresLevelsAndLocksAdvancedJobs()
    {
        var controller = new AllyProgressionController();
        for (var i = 1; i < 5; i++)
        {
            controller.TryLevelUp("mage", true, int.MaxValue, out _);
        }

        controller.Reset();

        Assert.That(controller.GetLevel("mage"), Is.EqualTo(1));
        Assert.That(controller.IsUnlocked("pyromancer"), Is.False);
        Assert.That(controller.IsUnlocked("mage"), Is.True);
    }
}
#endif
