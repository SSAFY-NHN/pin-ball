using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Pinball : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 8f;
    
    public BattleUnitSpawnData AllyData => new()
    {
        UnitId = "DefaultAlly",
        BaseStats = new BattleUnitStats
        {
            MaxHp = 24f,
            AttackDamage = 5f,
            AttackRate = 1.2f,
            AttackRange = 1.6f,
            MoveSpeed = 3.2f
        },
        Modifier = new BattleUnitModifier
        {
            MergeTier = 1,
            MergeAttackBonusPerTier = 0.2f,
            MergeHpBonusPerTier = 0.25f,
            EquipmentAttackBonus = 2f,
            EquipmentHpBonus = 4f
        }
    };
    private Rigidbody2D _rigidBody2D;

    private void Awake()
    {
        _rigidBody2D = GetComponent<Rigidbody2D>();
        
        Deactivate();
    }

    public void Activate(Vector2 worldPosition, Vector2 launchDirection)
    {
        transform.position = worldPosition;
        gameObject.SetActive(true);

        _rigidBody2D.simulated = true;
        _rigidBody2D.linearVelocity = Vector2.zero;
        _rigidBody2D.angularVelocity = 0f;
        
        if (launchDirection.sqrMagnitude < 0.001f)
        {
            launchDirection = Vector2.down;
        }

        _rigidBody2D.linearVelocity =
            launchDirection.normalized * launchSpeed;
    }
    
    public void Activate(Vector2 worldPosition)
    {
        var defaultDirection = Vector2.down;
        Activate(worldPosition, defaultDirection);
    }

    public void Deactivate()
    {
        if (_rigidBody2D != null)
        {
            _rigidBody2D.linearVelocity = Vector2.zero;
            _rigidBody2D.angularVelocity = 0f;
            _rigidBody2D.simulated = false;
        }

        gameObject.SetActive(false);
    }
}
