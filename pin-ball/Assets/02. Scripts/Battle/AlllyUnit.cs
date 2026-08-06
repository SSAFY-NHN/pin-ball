using UnityEngine;

public class AllyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Ally;
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
