using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PinballGoal : MonoBehaviour
{
    [SerializeField] private BattleUnitSpawnData unitData = new()
    {
        UnitId = "warrior",
        Level = 1
    };

    public BattleUnitSpawnData UnitData => unitData;

    private PinballManager _pinballManager;
    private BoxCollider2D _collider;
    private float _baseWidth;

    private void Awake()
    {
        if (unitData == null)
        {
            unitData = new BattleUnitSpawnData();
        }
        else if (unitData.UnitId == "DefaultAlly")
        {
            unitData.UnitId = "warrior";
        }

        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        _baseWidth = _collider.size.x;
    }

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.RegisterGoal(this);
    }

    private void OnDestroy()
    {
        _pinballManager?.UnregisterGoal(this);
    }

    private void OnMouseDown()
    {
        _pinballManager?.SelectGoal(this);
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _pinballManager?.SelectSwapGoal(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponentInParent<Pinball>();
        if (ball == null) return;

        _pinballManager.OnGoalBall(ball, this);
    }

    internal void SetWidthMultiplier(float multiplier, float maxWorldWidth)
    {
        var scaleX = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
        var targetWorldWidth = _baseWidth * scaleX * multiplier;

        if (maxWorldWidth < float.MaxValue)
        {
            targetWorldWidth = Mathf.Min(targetWorldWidth, maxWorldWidth);
        }

        var size = _collider.size;
        size.x = targetWorldWidth / scaleX;
        _collider.size = size;
    }

    internal void SetUnitData(BattleUnitSpawnData data)
    {
        unitData = data;
    }
}
