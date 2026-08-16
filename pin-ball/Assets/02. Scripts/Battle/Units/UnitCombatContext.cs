using System;

public sealed class UnitCombatContext
{
    public UnitTargetFinder TargetFinder { get; }
    public BattleAreaBounds BattleArea { get; }
    public Action<UnitBase> NotifyUnitDied { get; }
    public Action<UnitBase> NotifyUnitDamaged { get; }

    public UnitCombatContext(
        UnitTargetFinder targetFinder,
        BattleAreaBounds battleArea,
        Action<UnitBase> notifyUnitDied,
        Action<UnitBase> notifyUnitDamaged = null)
    {
        TargetFinder = targetFinder ?? throw new ArgumentNullException(nameof(targetFinder));
        BattleArea = battleArea ?? throw new ArgumentNullException(nameof(battleArea));
        NotifyUnitDied = notifyUnitDied ?? throw new ArgumentNullException(nameof(notifyUnitDied));
        NotifyUnitDamaged = notifyUnitDamaged;
    }
}
