using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Pinball : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 8f;
    [SerializeField, Min(0.1f)] private float maximumSpeed = 8.5f;
    [SerializeField, Min(0f)] private float linearDamping = 0.35f;
    [SerializeField, Min(0f)] private float gravityScale = 0.35f;

    public Vector2 Velocity => _rigidBody2D.linearVelocity;
    public float Diameter =>
        _collider.radius * 2f *
        Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

    internal bool IsClone { get; private set; }
    internal bool HasSplit { get; set; }
    internal int SmallPinHitCount { get; set; }
    internal int BigBumperHitCount { get; set; }
    internal int GoldenBumperGold { get; set; }
    internal int TargetMagnetUseCount { get; set; }
    internal int OverloadUseCount { get; set; }

    private PinballManager _manager;
    private Rigidbody2D _rigidBody2D;
    private CircleCollider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private PinballArcaneVfx _arcaneVfx;

    private void Awake()
    {
        EnsureInitialized();

        Deactivate();
    }

    private void FixedUpdate()
    {
        _manager?.ApplyTargetMagnet(this);
        _rigidBody2D.linearVelocity = PinballMotionMath.CapVelocity(
            _rigidBody2D.linearVelocity,
            maximumSpeed);
        _arcaneVfx?.OnVelocityChanged(_rigidBody2D.linearVelocity);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var contactPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : (Vector2)transform.position;

        var obstacle = collision.collider.GetComponentInParent<PinballObstacle>();
        float emphasis = obstacle == null
            ? 0.75f
            : obstacle.Type == EPinballObstacle.BigBumper ? 1.35f : 1f;
        _arcaneVfx?.PlayCollision(
            contactPoint,
            collision.relativeVelocity.magnitude,
            emphasis);
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
        bool isClone)
    {
        EnsureInitialized();
        ResetRunState(isClone);
        transform.position = worldPosition;
        gameObject.SetActive(true);
        _rigidBody2D.simulated = true;
        ResetPosition(worldPosition, launchDirection);
        _arcaneVfx?.OnActivated();
    }

    internal void LoadAt(Vector2 worldPosition)
    {
        EnsureInitialized();
        ResetRunState(false);
        transform.position = worldPosition;
        gameObject.SetActive(true);
        _rigidBody2D.linearVelocity = Vector2.zero;
        _rigidBody2D.angularVelocity = 0f;
        _rigidBody2D.simulated = false;
        _arcaneVfx?.OnDeactivated();
    }

    internal void LaunchLoaded(Vector2 launchVelocity)
    {
        EnsureInitialized();
        _rigidBody2D.simulated = true;
        _rigidBody2D.linearVelocity = launchVelocity;
        _rigidBody2D.angularVelocity = 0f;
        _arcaneVfx?.OnActivated();
        _arcaneVfx?.OnVelocityChanged(launchVelocity);
        _arcaneVfx?.PlayLaunch();
    }

    internal void PlayLoadedFeedback()
    {
        _arcaneVfx?.PlayLoaded();
    }

    internal void PlayLaunchCameraFeedback(float normalizedPull)
    {
        _arcaneVfx?.PlayLaunchCamera(normalizedPull);
    }

    internal void PlayGoldRewardFeedback(int amount)
    {
        _arcaneVfx?.PlayGoldReward(amount);
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
    }

    internal void SetVelocity(Vector2 velocity)
    {
        _rigidBody2D.linearVelocity = velocity;
        _arcaneVfx?.OnVelocityChanged(velocity);
    }

    internal void ApplyForce(Vector2 force)
    {
        if (_rigidBody2D == null || !_rigidBody2D.simulated) return;
        _rigidBody2D.AddForce(force, ForceMode2D.Force);
    }

    internal void ApplyImpulse(Vector2 impulse)
    {
        if (_rigidBody2D == null || !_rigidBody2D.simulated) return;
        _rigidBody2D.AddForce(impulse, ForceMode2D.Impulse);
    }

    public void Deactivate()
    {
        if (_rigidBody2D != null)
        {
            _rigidBody2D.linearVelocity = Vector2.zero;
            _rigidBody2D.angularVelocity = 0f;
            _rigidBody2D.simulated = false;
        }

        _arcaneVfx?.OnDeactivated();

        gameObject.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (_rigidBody2D == null)
        {
            _rigidBody2D = GetComponent<Rigidbody2D>();
            _rigidBody2D.linearDamping = linearDamping;
            _rigidBody2D.gravityScale = gravityScale;
        }

        if (_collider == null)
        {
            _collider = GetComponent<CircleCollider2D>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_arcaneVfx == null)
        {
            _arcaneVfx = GetComponent<PinballArcaneVfx>();
            if (_arcaneVfx == null)
            {
                _arcaneVfx = gameObject.AddComponent<PinballArcaneVfx>();
            }

            _arcaneVfx.Initialize(_spriteRenderer, _rigidBody2D);
        }
    }

    private void ResetRunState(bool isClone)
    {
        IsClone = isClone;
        HasSplit = isClone;
        SmallPinHitCount = 0;
        BigBumperHitCount = 0;
        GoldenBumperGold = 0;
        TargetMagnetUseCount = 0;
        OverloadUseCount = 0;
    }
}
