using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Pinball : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 8f;

    public Vector2 Velocity => _rigidBody2D.linearVelocity;
    public Vector2 PreviousVelocity { get; private set; }
    public float Diameter =>
        _collider.radius * 2f *
        Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

    internal int PaidLaunchCost { get; private set; }
    internal bool IsClone { get; private set; }
    internal bool WasRescued { get; set; }
    internal bool HasSplit { get; set; }
    internal int SmallPinHitCount { get; set; }
    internal int BigBumperHitCount { get; set; }
    internal int GoldenBallGold { get; set; }
    internal int GoldenBumperGold { get; set; }
    internal int TargetMagnetUseCount { get; set; }
    internal int OverloadUseCount { get; set; }

    private PinballManager _manager;
    private Rigidbody2D _rigidBody2D;
    private CircleCollider2D _collider;

    private void Awake()
    {
        EnsureInitialized();

        Deactivate();
    }

    private void FixedUpdate()
    {
        _manager?.ApplyTargetMagnet(this);
        PreviousVelocity = _rigidBody2D.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _manager.ApplyCollisionRetention(this, PreviousVelocity);

        var obstacle = collision.collider.GetComponentInParent<PinballObstacle>();
        if (obstacle == null) return;

        _manager.OnBallHit(this, obstacle.Type);
    }

    internal void SetManager(PinballManager manager)
    {
        EnsureInitialized();
        _manager = manager;
    }

    internal void Activate(
        Vector2 worldPosition,
        Vector2 launchDirection,
        int paidLaunchCost,
        bool isClone)
    {
        EnsureInitialized();

        PaidLaunchCost = paidLaunchCost;
        IsClone = isClone;
        WasRescued = false;
        HasSplit = isClone;
        SmallPinHitCount = 0;
        BigBumperHitCount = 0;
        GoldenBallGold = 0;
        GoldenBumperGold = 0;
        TargetMagnetUseCount = 0;
        OverloadUseCount = 0;

        transform.position = worldPosition;
        gameObject.SetActive(true);

        _rigidBody2D.simulated = true;
        ResetPosition(worldPosition, launchDirection);
    }

    internal void ResetPosition(Vector2 worldPosition, Vector2 launchDirection)
    {
        transform.position = worldPosition;
        _rigidBody2D.linearVelocity = Vector2.zero;
        _rigidBody2D.angularVelocity = 0f;

        if (launchDirection.sqrMagnitude < 0.001f)
        {
            launchDirection = Vector2.down;
        }

        _rigidBody2D.linearVelocity = launchDirection.normalized * launchSpeed;
        PreviousVelocity = _rigidBody2D.linearVelocity;
    }

    internal void SetVelocity(Vector2 velocity)
    {
        _rigidBody2D.linearVelocity = velocity;
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

    private void EnsureInitialized()
    {
        if (_rigidBody2D == null)
        {
            _rigidBody2D = GetComponent<Rigidbody2D>();
        }

        if (_collider == null)
        {
            _collider = GetComponent<CircleCollider2D>();
        }
    }
}
