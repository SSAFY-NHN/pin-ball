using System;

public sealed class BattleDefenseLineController
{
    public int AllyMaximumHp { get; private set; }
    public int EnemyMaximumHp { get; }

    private int allyHp;
    private int enemyHp;

    public BattleDefenseLineController(int allyMaximumHp, int enemyMaximumHp)
    {
        AllyMaximumHp = Math.Max(1, allyMaximumHp);
        EnemyMaximumHp = Math.Max(1, enemyMaximumHp);
        ResetForWave();
    }

    public void ResetForWave()
    {
        allyHp = AllyMaximumHp;
        enemyHp = EnemyMaximumHp;
    }

    public bool ApplyDamage(EBattleTeam team, int amount)
    {
        if (amount <= 0 || IsDestroyed(team)) return false;

        if (team == EBattleTeam.Ally)
        {
            allyHp = Math.Max(0, allyHp - amount);
        }
        else
        {
            enemyHp = Math.Max(0, enemyHp - amount);
        }

        return true;
    }

    public bool IncreaseAllyMaximumHp(int amount)
    {
        if (amount <= 0) return false;
        AllyMaximumHp += amount;
        allyHp += amount;
        return true;
    }

    public int GetCurrentHp(EBattleTeam team) =>
        team == EBattleTeam.Ally ? allyHp : enemyHp;

    public int GetMaximumHp(EBattleTeam team) =>
        team == EBattleTeam.Ally ? AllyMaximumHp : EnemyMaximumHp;

    public bool IsDestroyed(EBattleTeam team) => GetCurrentHp(team) <= 0;
}
