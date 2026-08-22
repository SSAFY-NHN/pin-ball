#if UNITY_EDITOR
using NUnit.Framework;

public sealed class BattleRunStateTests
{
    [Test]
    public void RunState_StartsAtFirstWaveWithThreeChances()
    {
        var waves = new[] { new BattleWaveData(), new BattleWaveData() };
        var state = new BattleRunState(waves, true, 3);

        Assert.That(state.CurrentWaveNumber, Is.EqualTo(1));
        Assert.That(state.TotalWaveCount, Is.EqualTo(2));
        Assert.That(state.PlayerHp, Is.EqualTo(3));
        Assert.That(state.MaximumPlayerHp, Is.EqualTo(3));
        Assert.That(state.State, Is.EqualTo(EWaveState.Pending));
    }

    [Test]
    public void ConsumeChance_StopsAtZeroWithoutChangingWave()
    {
        var state = new BattleRunState(
            new[] { new BattleWaveData(), new BattleWaveData() },
            true,
            3);

        Assert.That(state.ConsumeChance(), Is.True);
        Assert.That(state.ConsumeChance(), Is.True);
        Assert.That(state.ConsumeChance(), Is.True);
        Assert.That(state.ConsumeChance(), Is.False);
        Assert.That(state.PlayerHp, Is.Zero);
        Assert.That(state.CurrentWaveNumber, Is.EqualTo(1));
    }

    [TestCase(EWaveResolutionResult.Cleared, false, 2, EWaveState.Pending)]
    [TestCase(EWaveResolutionResult.Cleared, true, 2, EWaveState.Victory)]
    [TestCase(EWaveResolutionResult.Failed, false, 2, EWaveState.Pending)]
    [TestCase(EWaveResolutionResult.Failed, false, 0, EWaveState.Defeat)]
    public void ResolveNextState_UsesResultFinalWaveAndChances(
        EWaveResolutionResult result,
        bool isFinalWave,
        int remainingChances,
        EWaveState expected)
    {
        Assert.That(
            BattleResolutionPolicy.ResolveNextState(
                result,
                isFinalWave,
                remainingChances),
            Is.EqualTo(expected));
    }

    [TestCase(20, 20, false, EWaveResolutionResult.Cleared)]
    [TestCase(20, 0, true, EWaveResolutionResult.Cleared)]
    [TestCase(0, 20, true, EWaveResolutionResult.Failed)]
    public void TryResolveDefenseLines_UsesDefenseHpOnly(
        int allyDefenseHp,
        int enemyDefenseHp,
        bool expectedResolved,
        EWaveResolutionResult expectedResult)
    {
        Assert.That(
            BattleResolutionPolicy.TryResolveDefenseLines(
                allyDefenseHp,
                enemyDefenseHp,
                out EWaveResolutionResult result),
            Is.EqualTo(expectedResolved));
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [TestCase("RetryGoldReward")]
    [TestCase("WaveClearGoldReward")]
    [TestCase("FinalClearGoldReward")]
    public void BattleWaveData_DoesNotExposeGoldRewardField(string fieldName)
    {
        Assert.That(typeof(BattleWaveData).GetField(fieldName), Is.Null);
    }
}
#endif
