public enum EBattleAttackStyle
{
    Melee,
    Arrow,
    Magic
}

public static class BattleFeedbackMath
{
    public static EBattleAttackStyle ResolveAttackStyle(
        float attackRange,
        string unitId)
    {
        if (attackRange <= 2.5f) return EBattleAttackStyle.Melee;

        string id = unitId ?? string.Empty;
        return id.Contains("archer") ||
               id.Contains("ranger") ||
               id.Contains("marksman")
            ? EBattleAttackStyle.Arrow
            : EBattleAttackStyle.Magic;
    }
}
