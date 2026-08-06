using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BattleUnitVisual : MonoBehaviour
{
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] moveFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 10f;
    [SerializeField] private bool sourceFacesRight;

    private UnitBase _unit;
    private SpriteRenderer _spriteRenderer;
    private EBattleUnitState _previousState;
    private Vector3 _previousPosition;
    private float _stateStartedAt;

    private void Awake()
    {
        _unit = GetComponent<UnitBase>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _previousPosition = transform.position;
        _previousState = _unit != null ? _unit.State : EBattleUnitState.Idle;
        _stateStartedAt = Time.time;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = 10;
        }
    }

    private void LateUpdate()
    {
        if (_unit == null || _spriteRenderer == null)
        {
            return;
        }

        var state = _unit.State;
        if (state != _previousState)
        {
            _previousState = state;
            _stateStartedAt = Time.time;
        }

        var frames = GetFrames(state);
        if (frames.Length > 0)
        {
            var elapsed = Mathf.Max(0f, Time.time - _stateStartedAt);
            var frameIndex = Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length;
            _spriteRenderer.sprite = frames[frameIndex];
        }

        UpdateFacing();
    }

    private Sprite[] GetFrames(EBattleUnitState state)
    {
        if (state == EBattleUnitState.Moving && moveFrames != null && moveFrames.Length > 0)
        {
            return moveFrames;
        }

        if (state == EBattleUnitState.Attacking && attackFrames != null && attackFrames.Length > 0)
        {
            return attackFrames;
        }

        return idleFrames ?? System.Array.Empty<Sprite>();
    }

    private void UpdateFacing()
    {
        var movement = transform.position - _previousPosition;
        _previousPosition = transform.position;

        var facesRight = _unit.Team == EBattleTeam.Enemy;
        if (Mathf.Abs(movement.x) > 0.0001f)
        {
            facesRight = movement.x > 0f;
        }

        _spriteRenderer.flipX = sourceFacesRight != facesRight;
    }

#if UNITY_EDITOR
    public void Configure(
        Sprite[] idle,
        Sprite[] moving,
        Sprite[] attacking,
        float animationFramesPerSecond,
        bool spritesFaceRight)
    {
        idleFrames = idle;
        moveFrames = moving;
        attackFrames = attacking;
        framesPerSecond = Mathf.Max(1f, animationFramesPerSecond);
        sourceFacesRight = spritesFaceRight;
    }
#endif
}
