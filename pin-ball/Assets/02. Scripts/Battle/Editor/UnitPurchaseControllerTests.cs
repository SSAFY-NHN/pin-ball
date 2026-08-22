#if UNITY_EDITOR
using NUnit.Framework;

public sealed class UnitPurchaseControllerTests
{
    private BattleEconomy economy;
    private UnitPurchaseController controller;

    [SetUp]
    public void SetUp()
    {
        economy = new BattleEconomy(100);
        controller = new UnitPurchaseController(
            economy,
            new UnitPurchaseSettings("warrior", 30, 1.4f),
            new UnitPurchaseSettings("archer", 35, 1.4f),
            new UnitPurchaseSettings("mage", 40, 1.4f));
    }

    [TestCase("warrior", 30, 70, 42)]
    [TestCase("archer", 35, 65, 49)]
    [TestCase("mage", 40, 60, 56)]
    public void TryPurchase_SpawnsSelectedUnitAndRecordsOnlyItsPurchase(
        string unitId,
        int expectedCost,
        int expectedGold,
        int expectedNextCost)
    {
        string spawnedUnitId = null;

        bool purchased = controller.TryPurchase(
            unitId,
            true,
            spawnData =>
            {
                spawnedUnitId = spawnData.UnitId;
                return true;
            },
            out UnitPurchaseResult result);

        Assert.That(purchased, Is.True);
        Assert.That(spawnedUnitId, Is.EqualTo(unitId));
        Assert.That(economy.Gold, Is.EqualTo(expectedGold));
        Assert.That(result.UnitId, Is.EqualTo(unitId));
        Assert.That(result.PurchaseCount, Is.EqualTo(1));
        Assert.That(result.Cost, Is.EqualTo(expectedCost));
        Assert.That(controller.GetNextCost(unitId), Is.EqualTo(expectedNextCost));

        foreach (string otherUnitId in new[] { "warrior", "archer", "mage" })
        {
            if (otherUnitId == unitId) continue;
            Assert.That(controller.GetPurchaseCount(otherUnitId), Is.Zero);
        }
    }

    [Test]
    public void TryPurchase_InsufficientGoldDoesNotSpawnOrMutateState()
    {
        var poorEconomy = new BattleEconomy(29);
        var poorController = CreateController(poorEconomy);
        bool spawnAttempted = false;

        bool purchased = poorController.TryPurchase(
            "warrior",
            true,
            _ =>
            {
                spawnAttempted = true;
                return true;
            },
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(spawnAttempted, Is.False);
        Assert.That(poorEconomy.Gold, Is.EqualTo(29));
        Assert.That(poorController.GetPurchaseCount("warrior"), Is.Zero);
    }

    [Test]
    public void TryPurchase_DeploymentLimitDoesNotSpawnOrMutateState()
    {
        bool spawnAttempted = false;

        bool purchased = controller.TryPurchase(
            "warrior",
            false,
            _ =>
            {
                spawnAttempted = true;
                return true;
            },
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(spawnAttempted, Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(controller.GetPurchaseCount("warrior"), Is.Zero);
    }

    [Test]
    public void TryPurchase_SpawnFailureDoesNotSpendOrRecordPurchase()
    {
        bool purchased = controller.TryPurchase(
            "mage",
            true,
            _ => false,
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(controller.GetPurchaseCount("mage"), Is.Zero);
        Assert.That(controller.GetNextCost("mage"), Is.EqualTo(40));
    }

    [Test]
    public void TryPurchase_InvalidUnitDoesNotSpawnOrMutateState()
    {
        bool spawnAttempted = false;

        bool purchased = controller.TryPurchase(
            "spearman",
            true,
            _ =>
            {
                spawnAttempted = true;
                return true;
            },
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(spawnAttempted, Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(controller.GetPurchaseCount("spearman"), Is.Zero);
    }

    [Test]
    public void TryPurchase_NullUnitIdDoesNotThrowOrMutateState()
    {
        bool spawnAttempted = false;

        bool purchased = controller.TryPurchase(
            null,
            true,
            _ =>
            {
                spawnAttempted = true;
                return true;
            },
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(spawnAttempted, Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }

    [Test]
    public void TryPurchase_InvalidSettingsDoNotSpawnOrMutateState()
    {
        var invalidController = new UnitPurchaseController(
            economy,
            new UnitPurchaseSettings("warrior", 30, 0.9f));
        bool spawnAttempted = false;

        bool purchased = invalidController.TryPurchase(
            "warrior",
            true,
            _ =>
            {
                spawnAttempted = true;
                return true;
            },
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(spawnAttempted, Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(invalidController.GetPurchaseCount("warrior"), Is.Zero);
    }

    [Test]
    public void RecordSuccessfulPurchase_IncreasesSelectedUnitCostWithoutSpendingGold()
    {
        Assert.That(controller.RecordSuccessfulPurchase("mage"), Is.True);

        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(controller.GetPurchaseCount("mage"), Is.EqualTo(1));
        Assert.That(controller.GetNextCost("mage"), Is.EqualTo(56));
        Assert.That(controller.GetNextCost("warrior"), Is.EqualTo(30));
        Assert.That(controller.GetNextCost("archer"), Is.EqualTo(35));
    }

    [Test]
    public void TryPurchaseFree_SpawnSuccessKeepsGoldAndRecordsSelectedUnit()
    {
        string spawnedUnitId = null;

        bool purchased = controller.TryPurchaseFree(
            "mage",
            true,
            spawnData =>
            {
                spawnedUnitId = spawnData.UnitId;
                return true;
            },
            out UnitPurchaseResult result);

        Assert.That(purchased, Is.True);
        Assert.That(spawnedUnitId, Is.EqualTo("mage"));
        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(result.Cost, Is.Zero);
        Assert.That(result.PurchaseCount, Is.EqualTo(1));
        Assert.That(controller.GetNextCost("mage"), Is.EqualTo(56));
        Assert.That(controller.GetNextCost("warrior"), Is.EqualTo(30));
    }

    [Test]
    public void TryPurchaseFree_SpawnFailureKeepsGoldAndPurchaseCount()
    {
        bool purchased = controller.TryPurchaseFree(
            "archer",
            true,
            _ => false,
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(controller.GetPurchaseCount("archer"), Is.Zero);
        Assert.That(controller.GetNextCost("archer"), Is.EqualTo(35));
    }

    [TestCase("missing", true)]
    [TestCase("warrior", false)]
    public void TryPurchaseFree_InvalidRequestDoesNotAttemptSpawn(
        string unitId,
        bool canDeploy)
    {
        bool spawnAttempted = false;

        bool purchased = controller.TryPurchaseFree(
            unitId,
            canDeploy,
            _ =>
            {
                spawnAttempted = true;
                return true;
            },
            out _);

        Assert.That(purchased, Is.False);
        Assert.That(spawnAttempted, Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }

    private static UnitPurchaseController CreateController(BattleEconomy targetEconomy)
    {
        return new UnitPurchaseController(
            targetEconomy,
            new UnitPurchaseSettings("warrior", 30, 1.4f),
            new UnitPurchaseSettings("archer", 35, 1.4f),
            new UnitPurchaseSettings("mage", 40, 1.4f));
    }
}
#endif
