using UnityEngine;

public class DefenseLineTrigger : MonoBehaviour
{
    [SerializeField] private EBattleTeam defenseTeam;

    public EBattleTeam DefenseTeam => defenseTeam;

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitBase unit = other.GetComponentInParent<UnitBase>();
        if (unit == null || unit.Team == defenseTeam) return;

        unit.ReachDefenseLine(defenseTeam);
    }
}
