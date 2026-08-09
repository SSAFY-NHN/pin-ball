using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PinballMagnetController : MonoBehaviour
{
    [SerializeField] private PinballManager pinballManager;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField, Min(0.1f)] private float effectRadius = 2.4f;
    [SerializeField, Min(0f)] private float force = 18f;
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.4f;
    [SerializeField, Min(0f)] private float cooldown = 1.5f;
    [SerializeField] private Color activeColor = new(0.45f, 1f, 1f, 1f);

    private Color _readyColor = Color.white;
    private float _activeUntil;
    private float _readyAt;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer != null) _readyColor = targetRenderer.color;
    }

    private void FixedUpdate()
    {
        var isActive = Time.time < _activeUntil;
        if (targetRenderer != null)
        {
            targetRenderer.color = isActive ? activeColor : _readyColor;
        }

        if (!isActive || pinballManager == null) return;

        foreach (var ball in pinballManager.ActiveBalls)
        {
            if (ball == null) continue;
            var offset = (Vector2)transform.position - (Vector2)ball.transform.position;
            var distance = offset.magnitude;
            if (distance <= 0.001f || distance > effectRadius) continue;

            var strength = force * (1f - distance / effectRadius);
            ball.ApplyForce(offset.normalized * strength);
        }
    }

    private void OnMouseDown()
    {
        if (Time.time < _readyAt) return;
        _activeUntil = Time.time + pulseDuration;
        _readyAt = Time.time + cooldown;
    }
}
