using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UnitSkillContext
{
    public UnitBase Caster { get; }
    public UnitBase PrimaryTarget { get; }
    public UnitTargetFinder TargetFinder { get; }
    public IEnemyBattleActions EnemyActions { get; }
    public List<UnitBase> Targets { get; } = new();

    public UnitSkillContext(UnitBase caster, UnitBase primaryTarget, UnitTargetFinder targetFinder, IEnemyBattleActions enemyActions = null)
    {
        Caster = caster != null ? caster : throw new ArgumentNullException(nameof(caster));
        PrimaryTarget = primaryTarget;
        TargetFinder = targetFinder ?? throw new ArgumentNullException(nameof(targetFinder));
        EnemyActions = enemyActions;
    }
}
