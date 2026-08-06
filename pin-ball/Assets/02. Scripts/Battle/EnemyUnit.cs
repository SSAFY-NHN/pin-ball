using UnityEngine;

public class EnemyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Enemy;
    protected override Color IdleColor => Color.white;

    protected override void Tick()
    {
        if (TryKeepOrAcquireTarget())
        {
            MoveOrAttackTarget();
            return;
        }

        _state = EBattleUnitState.Idle;
        ClearTarget();
    }
}
