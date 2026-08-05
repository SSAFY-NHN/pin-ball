using UnityEngine;

public class AllyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Ally;
    protected override Color IdleColor => new(0.3f, 0.8f, 1f, 1f);
    
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
