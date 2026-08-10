using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PinballMagnetController : MonoBehaviour
{
    [SerializeField] private PinballManager pinballManager;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField, Min(0.1f)] private float effectRadius = 2.4f;
    [SerializeField, Min(0f)] private float force = 18f;
    [SerializeField] private Color activeColor = new(0.45f, 1f, 1f, 1f);

    private Color _readyColor = Color.white;
    private bool _isActive;
    private ArcaneMaskGlowController _glow;
    private ArcaneSpriteEffect _arcEffect;
    private ArcaneSpriteEffect _sparkEffect;
    private Material _effectMaterial;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer != null) _readyColor = targetRenderer.color;
        var catalog = ArcaneVfxCatalog.Load();
        var shader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        if (catalog == null || targetRenderer == null) return;

        _glow = GetComponent<ArcaneMaskGlowController>();
        if (shader == null) return;

        _effectMaterial = new Material(shader)
        {
            name = "Arcane Magnet VFX (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _effectMaterial.SetFloat("_Intensity", 1.8f);
        _effectMaterial.SetFloat("_GlowSpread", 1.25f);
        _arcEffect = CreateEffect("Magnet Arc", catalog.magnetArc, targetRenderer.sortingOrder + 2);
        _sparkEffect = CreateEffect("Magnet Spark", catalog.magnetSpark, targetRenderer.sortingOrder + 3);
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
        _arcEffect?.Play(transform.position, activationVfxDuration, Vector3.one * 0.65f,
            Vector3.one * 0.9f, new Color(0.03f, 0.65f, 1f, 0.9f));
        _sparkEffect?.Play(transform.position, 0.22f, Vector3.one * 0.25f,
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

    private ArcaneSpriteEffect CreateEffect(string effectName, Sprite[] sprites, int sortingOrder)
    {
        var child = new GameObject(effectName);
        child.transform.SetParent(transform, false);
        var effect = child.AddComponent<ArcaneSpriteEffect>();
        effect.Initialize(sprites, _effectMaterial, sortingOrder);
        return effect;
    }

    private void OnDestroy()
    {
        if (_effectMaterial != null) Destroy(_effectMaterial);
    }
}
