using UnityEngine;

public class PinballReflectorController : MonoBehaviour
{
    [SerializeField] private Vector2 outwardNormal = Vector2.up;
    [SerializeField, Min(0f)] private float impulse = 2.5f;
    [SerializeField, Min(0.1f)] private float activationRadius = 2f;
    [SerializeField, Min(1f)] private float flickAngle = 28f;
    [SerializeField, Min(1f)] private float rotationSpeed = 360f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.12f;
    [SerializeField, Min(0f)] private float cooldown = 0.45f;

    private PinballManager _pinballManager;
    private float _restAngle;
    private float _activeUntil;
    private float _readyAt;
    private ArcaneMaskGlowController _glow;

    private void Awake()
    {
        _pinballManager = Object.FindFirstObjectByType<PinballManager>();
        _restAngle = transform.localEulerAngles.z;
        var renderer = GetComponent<SpriteRenderer>();
        var catalog = ArcaneVfxCatalog.Load();
        if (renderer != null && catalog != null)
        {
            _glow = ArcaneMaskGlowController.Attach(renderer, catalog.reflectorMask);
        }
    }

    private void FixedUpdate()
    {
        if (Time.time >= _readyAt && HasBallInRange())
        {
            _activeUntil = Time.time + activeDuration;
            _readyAt = Time.time + cooldown;
            _glow?.Pulse(2.2f, activeDuration + 0.1f);
        }

        var directionSign = Mathf.Approximately(outwardNormal.x, 0f)
            ? 1f
            : Mathf.Sign(outwardNormal.x);
        var targetAngle = Time.time < _activeUntil
            ? _restAngle + flickAngle * directionSign
            : _restAngle;
        var currentAngle = transform.localEulerAngles.z;
        var nextAngle = Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            rotationSpeed * Time.fixedDeltaTime);
        transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, nextAngle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var ball = collision.collider.GetComponentInParent<Pinball>();
        if (ball == null) return;

        var direction = transform.TransformDirection(outwardNormal).normalized;
        ball.ApplyImpulse(direction * impulse);
    }

    private bool HasBallInRange()
    {
        if (_pinballManager == null) return false;

        var squareRadius = activationRadius * activationRadius;
        foreach (var ball in _pinballManager.ActiveBalls)
        {
            if (ball == null) continue;
            if (((Vector2)ball.transform.position - (Vector2)transform.position).sqrMagnitude <= squareRadius)
            {
                return true;
            }
        }

        return false;
    }
}
