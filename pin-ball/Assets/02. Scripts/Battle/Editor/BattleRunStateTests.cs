#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class BattleRunStateTests
{
    [Test]
    public void AdvanceWave_MovesIndexUntilLastWaveOnly()
    {
        var waves = new List<BattleWaveData>
        {
            new(),
            new()
        };
        var state = new BattleRunState(waves, true, 20);

        Assert.That(state.AdvanceWave(), Is.True);
        Assert.That(state.CurrentWaveIndex, Is.EqualTo(1));
        Assert.That(state.AdvanceWave(), Is.False);
        Assert.That(state.CurrentWaveIndex, Is.EqualTo(1));
    }

    [Test]
    public void ApplyPlayerDamage_ClampsHpAtZero()
    {
        var state = new BattleRunState(new[] { new BattleWaveData() }, true, 20);
        Assert.That(state.ApplyPlayerDamage(30), Is.True);
        Assert.That(state.PlayerHp, Is.Zero);
    }
}
#endif
