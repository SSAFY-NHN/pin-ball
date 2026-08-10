public static class UnitStatsValidator
{
    public static bool IsValid(BattleUnitStats stats)
    {
        return stats.MaxHp > 0f &&
               stats.AttackDamage >= 0f &&
               stats.AttackRate > 0f &&
               stats.AttackRange > 0f &&
               stats.MoveSpeed >= 0f;
    }
}
