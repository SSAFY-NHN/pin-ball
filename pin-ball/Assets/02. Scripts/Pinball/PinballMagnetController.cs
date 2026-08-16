using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PinballMagnetController : MonoBehaviour
{
    [SerializeField] private PinballManager pinballManager;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField, Min(0.1f)] private float effectRadius = 2.4f;
    [SerializeField, Min(0f)] private float force = 18f;
    [SerializeField] private Color activeColor = new(0.45f, 1f, 1f, 1f);
    [SerializeField] private Material effectMaterial;
    [SerializeField] private ArcaneSpriteEffect arcEffect;
    [SerializeField] private ArcaneSpriteEffect sparkEffect;

    private Color _readyColor = Color.white;
    private bool _isActive;
    private ArcaneMaskGlowController _glow;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer != null) _readyColor = targetRenderer.color;
        var catalog = ArcaneVfxCatalog.Load();
        if (catalog == null || targetRenderer == null) return;

        _glow = GetComponent<ArcaneMaskGlowController>();
        arcEffect?.Initialize(
            catalog.magnetArc,
            effectMaterial,
            targetRenderer.sortingOrder + 2);
        sparkEffect?.Initialize(
            catalog.magnetSpark,
            effectMaterial,
            targetRenderer.sortingOrder + 3);
    }

    private void FixedUpdate()
    {
        if (targetRenderer != null)
        {
            targetRenderer.color = _isActive ? activeColor : _readyColor;
        }
        _glow?.SetActiveIntensity(_isActive ? 2f : 1.45f);

        if (!_isActive || pinballManager == null) return;

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
        if (_isActive) return;

        const float activationVfxDuration = 0.4f;
        _isActive = true;
        _glow?.Pulse(2.4f, activationVfxDuration);
        arcEffect?.Play(transform.position, activationVfxDuration, Vector3.one * 0.65f,
            Vector3.one * 0.9f, new Color(0.03f, 0.65f, 1f, 0.9f));
        sparkEffect?.Play(transform.position, 0.22f, Vector3.one * 0.25f,
            Vector3.one * 0.7f, new Color(0.55f, 0.12f, 1f, 1f));
    }

    private void OnMouseUp()
    {
        _isActive = false;
    }

    private void OnDisable()
    {
        _isActive = false;
    }

}
