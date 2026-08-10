#if UNITY_EDITOR
using NUnit.Framework;

public class WaveResolutionTests
{
    [Test]
    public void TryBegin_StoresResultAndRequiresFullDelay()
    {
        var state = new WaveResolutionState();

        Assert.That(
            state.TryBegin(EWaveResolutionResult.Cleared, 3, 10f, 2f),
            Is.True);
        Assert.That(state.Result, Is.EqualTo(EWaveResolutionResult.Cleared));
        Assert.That(state.WaveNumber, Is.EqualTo(3));
        Assert.That(state.IsElapsed(11.999f), Is.False);
        Assert.That(state.IsElapsed(12f), Is.True);
    }

    [Test]
    public void TryBegin_RejectsDuplicateUntilCleared()
    {
        var state = new WaveResolutionState();

        Assert.That(
            state.TryBegin(EWaveResolutionResult.Failed, 1, 0f, 2f),
            Is.True);
        Assert.That(
            state.TryBegin(EWaveResolutionResult.Cleared, 1, 0f, 2f),
            Is.False);
        state.Clear();
        Assert.That(
            state.TryBegin(EWaveResolutionResult.Cleared, 2, 3f, 2f),
            Is.True);
    }

    [TestCase(0, 0, true, EWaveResolutionResult.Cleared)]
    [TestCase(0, 2, true, EWaveResolutionResult.Failed)]
    [TestCase(2, 0, true, EWaveResolutionResult.Cleared)]
    [TestCase(2, 2, false, EWaveResolutionResult.Cleared)]
    public void TryDetectWipe_UsesEnemyFirstTiePriority(
        int allies,
        int enemies,
        bool expectedDetected,
        EWaveResolutionResult expectedResult)
    {
        bool detected = BattleResolutionPolicy.TryDetectWipe(
            allies,
            enemies,
            out EWaveResolutionResult result);

        Assert.That(detected, Is.EqualTo(expectedDetected));
        if (detected) Assert.That(result, Is.EqualTo(expectedResult));
    }

    [TestCase(EWaveResolutionResult.Cleared, false, 20, EWaveState.Pending)]
    [TestCase(EWaveResolutionResult.Cleared, true, 20, EWaveState.Victory)]
    [TestCase(EWaveResolutionResult.Failed, false, 1, EWaveState.Pending)]
    [TestCase(EWaveResolutionResult.Failed, false, 0, EWaveState.Defeat)]
    public void ResolveNextState_UsesOutcomeWavePositionAndHp(
        EWaveResolutionResult result,
        bool isFinalWave,
        int playerHp,
        EWaveState expected)
    {
        Assert.That(
            BattleResolutionPolicy.ResolveNextState(
                result,
                isFinalWave,
                playerHp),
            Is.EqualTo(expected));
    }
}
#endif
