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
            new UnitPurchaseSettings("warrior", 30, 1.4f, 4f),
            new UnitPurchaseSettings("archer", 35, 1.4f, 5f),
            new UnitPurchaseSettings("mage", 40, 1.4f, 7f),
            new UnitPurchaseSettings("spearman", 35, 1.4f, 5f));
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

        foreach (string otherUnitId in new[]
                 { "warrior", "archer", "mage", "spearman" })
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
            "missing",
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
        Assert.That(controller.GetPurchaseCount("missing"), Is.Zero);
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
        Assert.That(controller.GetRemainingCooldown("archer"), Is.Zero);
    }

    [TestCase("warrior", 4f)]
    [TestCase("archer", 5f)]
    [TestCase("mage", 7f)]
    [TestCase("spearman", 5f)]
    public void TryPurchase_SuccessStartsOnlySelectedUnitCooldown(
        string unitId,
        float expectedCooldown)
    {
        Assert.That(
            controller.TryPurchase(unitId, true, _ => true, out _),
            Is.True);

        Assert.That(
            controller.GetRemainingCooldown(unitId),
            Is.EqualTo(expectedCooldown));
        foreach (string otherUnitId in new[]
                 { "warrior", "archer", "mage", "spearman" })
        {
            if (otherUnitId == unitId) continue;
            Assert.That(controller.GetRemainingCooldown(otherUnitId), Is.Zero);
        }
    }

    [Test]
    public void Advance_DecreasesCooldownAndBlocksPaidAndFreePurchase()
    {
        controller.TryPurchase("warrior", true, _ => true, out _);

        Assert.That(controller.CanPurchase("warrior", true), Is.False);
        Assert.That(controller.CanPurchaseFree("warrior", true), Is.False);
        controller.Advance(1.5f);
        Assert.That(controller.GetRemainingCooldown("warrior"), Is.EqualTo(2.5f));
        controller.Advance(2.5f);
        Assert.That(controller.CanPurchaseFree("warrior", true), Is.True);
    }

    [Test]
    public void TryPurchaseFree_SpearmanSuccessStartsCooldownWithoutSpendingGold()
    {
        Assert.That(
            controller.TryPurchaseFree("spearman", true, _ => true, out var result),
            Is.True);

        Assert.That(economy.Gold, Is.EqualTo(100));
        Assert.That(result.UnitId, Is.EqualTo("spearman"));
        Assert.That(controller.GetPurchaseCount("spearman"), Is.EqualTo(1));
        Assert.That(controller.GetRemainingCooldown("spearman"), Is.EqualTo(5f));
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

    [Test]
    public void ResetForWave_ClearsEveryPurchaseCountAndCooldownWithoutChangingGold()
    {
        Assert.That(
            controller.TryPurchase("warrior", true, _ => true, out _),
            Is.True);
        Assert.That(
            controller.TryPurchaseFree("mage", true, _ => true, out _),
            Is.True);
        int goldBeforeReset = economy.Gold;

        controller.ResetForWave();

        Assert.That(economy.Gold, Is.EqualTo(goldBeforeReset));
        AssertPurchaseState("warrior", 30);
        AssertPurchaseState("archer", 35);
        AssertPurchaseState("mage", 40);
        AssertPurchaseState("spearman", 35);
    }

    private void AssertPurchaseState(string unitId, int expectedBaseCost)
    {
        Assert.That(controller.GetPurchaseCount(unitId), Is.Zero);
        Assert.That(controller.GetNextCost(unitId), Is.EqualTo(expectedBaseCost));
        Assert.That(controller.GetRemainingCooldown(unitId), Is.Zero);
    }

    private static UnitPurchaseController CreateController(BattleEconomy targetEconomy)
    {
        return new UnitPurchaseController(
            targetEconomy,
            new UnitPurchaseSettings("warrior", 30, 1.4f, 4f),
            new UnitPurchaseSettings("archer", 35, 1.4f, 5f),
            new UnitPurchaseSettings("mage", 40, 1.4f, 7f),
            new UnitPurchaseSettings("spearman", 35, 1.4f, 5f));
    }
}
#endif
