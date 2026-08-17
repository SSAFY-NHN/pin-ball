using UnityEngine;

public class DefenseLineTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyUnit enemy = other.GetComponentInParent<EnemyUnit>();
        if (enemy == null) return;

        App.Get<BattleManager>().TryResolveEnemyBreach(enemy);
    }
}
