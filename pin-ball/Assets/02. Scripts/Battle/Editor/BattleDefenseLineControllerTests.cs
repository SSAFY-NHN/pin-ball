#if UNITY_EDITOR
using NUnit.Framework;

public sealed class BattleDefenseLineControllerTests
{
    [Test]
    public void ResetForWave_RestoresBothLinesToMaximum()
    {
        var controller = new BattleDefenseLineController(20, 20);
        controller.ApplyDamage(EBattleTeam.Ally, 7);
        controller.ApplyDamage(EBattleTeam.Enemy, 9);

        controller.ResetForWave();

        Assert.That(controller.GetCurrentHp(EBattleTeam.Ally), Is.EqualTo(20));
        Assert.That(controller.GetCurrentHp(EBattleTeam.Enemy), Is.EqualTo(20));
    }

    [Test]
    public void ApplyDamage_ClampsAtZeroAndReportsDestroyedLine()
    {
        var controller = new BattleDefenseLineController(20, 20);

        Assert.That(controller.ApplyDamage(EBattleTeam.Enemy, 25), Is.True);
        Assert.That(controller.GetCurrentHp(EBattleTeam.Enemy), Is.Zero);
        Assert.That(controller.IsDestroyed(EBattleTeam.Enemy), Is.True);
        Assert.That(controller.ApplyDamage(EBattleTeam.Enemy, 1), Is.False);
    }

    [Test]
    public void IncreaseAllyMaximumHp_DoesNotChangeEnemyMaximum()
    {
        var controller = new BattleDefenseLineController(20, 20);

        Assert.That(controller.IncreaseAllyMaximumHp(10), Is.True);
        Assert.That(controller.GetMaximumHp(EBattleTeam.Ally), Is.EqualTo(30));
        Assert.That(controller.GetCurrentHp(EBattleTeam.Ally), Is.EqualTo(30));
        Assert.That(controller.GetMaximumHp(EBattleTeam.Enemy), Is.EqualTo(20));
    }
}
#endif
