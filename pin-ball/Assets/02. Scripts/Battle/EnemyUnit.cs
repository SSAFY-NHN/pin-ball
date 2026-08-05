using UnityEngine;

public class EnemyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Enemy;
    protected override Color IdleColor => new(1f, 0.45f, 0.45f, 1f);

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
