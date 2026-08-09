using UnityEngine;

/// <summary>
/// Lightweight, self-contained feedback for pooled pinballs. All renderers,
/// materials, and particle storage are created once and reused.
/// </summary>
[DisallowMultipleComponent]
public sealed class PinballArcaneVfx : MonoBehaviour
{
    private const float ReferenceSpeed = 8f;
    private static readonly Color TrailLow = new(0.02f, 0.55f, 1f, 0.78f);
    private static readonly Color TrailHigh = new(0.55f, 0.12f, 1f, 0.9f);

    private SpriteRenderer _sourceRenderer;
    private Rigidbody2D _body;
    private ArcaneMaskGlowController _glow;
    private TrailRenderer _trail;
    private ArcaneSpriteEffect _impact;
    private ArcaneSpriteEffect _ring;
    private Material _additiveMaterial;
    private bool _initialized;

    public void Initialize(SpriteRenderer sourceRenderer, Rigidbody2D body)
    {
        if (_initialized) return;

        _sourceRenderer = sourceRenderer;
        _body = body;

        var additiveShader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        var catalog = ArcaneVfxCatalog.Load();
        if (additiveShader == null || catalog == null)
        {
            Debug.LogWarning("Arcane pinball shader or VFX catalog was not found in Resources.", this);
            enabled = false;
            return;
        }

        _additiveMaterial = new Material(additiveShader)
        {
            name = "Arcane Additive (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _additiveMaterial.SetFloat("_Intensity", 1.65f);
        _additiveMaterial.SetFloat("_GlowSpread", 1.2f);
        _glow = ArcaneMaskGlowController.Attach(_sourceRenderer, catalog.ballMask);
        CreateTrail(catalog.ballTrail);
        _impact = CreateSpriteEffect("Arcane Impact", catalog.ballImpact, _sourceRenderer.sortingOrder + 3);
        _ring = CreateSpriteEffect("Arcane Ring", catalog.ballRing, _sourceRenderer.sortingOrder + 2);
        _initialized = true;
        OnDeactivated();
    }

    public void OnActivated()
    {
        if (!_initialized) return;

        _glow?.SetActiveIntensity(1.65f);
        _trail.Clear();
        _trail.emitting = true;
        OnVelocityChanged(_body != null ? _body.linearVelocity : Vector2.zero);
    }

    public void OnDeactivated()
    {
        if (!_initialized) return;

        _trail.emitting = false;
        _trail.Clear();
        _glow?.SetActiveIntensity(1.15f);
        _impact?.StopEffect();
        _ring?.StopEffect();
    }

    public void OnVelocityChanged(Vector2 velocity)
    {
        if (!_initialized || !_trail.emitting) return;

        var speed01 = Mathf.Clamp01(velocity.magnitude / (ReferenceSpeed * 1.5f));
        _trail.time = Mathf.Lerp(0.08f, 0.2f, speed01);
        _trail.widthMultiplier = Mathf.Lerp(0.28f, 0.6f, speed01);

        var hdrColor = Color.Lerp(TrailLow, TrailHigh, speed01);
        _trail.startColor = hdrColor;
        _trail.endColor = new Color(hdrColor.r * 0.2f, hdrColor.g * 0.2f, hdrColor.b * 0.2f, 0f);

        _glow?.SetActiveIntensity(Mathf.Lerp(1.35f, 1.9f, speed01));
    }

    public void PlayCollision(Vector2 worldPoint, float relativeSpeed)
    {
        if (!_initialized || !isActiveAndEnabled) return;

        var strength = Mathf.Clamp01(relativeSpeed / (ReferenceSpeed * 1.25f));
        var color = Color.Lerp(TrailLow, TrailHigh, strength);
        var position = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
        _impact.Play(position, 0.18f, Vector3.one * 0.28f, Vector3.one * 0.65f, color);
        _ring.Play(position, 0.24f, Vector3.one * 0.2f, Vector3.one * 0.9f, color);
        _glow?.Pulse(2f, 0.16f);
    }

    private void CreateTrail(Sprite[] trailSprites)
    {
        _trail = gameObject.AddComponent<TrailRenderer>();
        var trailMaterial = new Material(_additiveMaterial)
        {
            name = "Arcane Ball Trail (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        trailMaterial.SetFloat("_Intensity", 3f);
        if (trailSprites != null && trailSprites.Length > 0)
        {
            var sprite = trailSprites[0];
            trailMaterial.mainTexture = sprite.texture;
            var rect = sprite.textureRect;
            trailMaterial.mainTextureScale = new Vector2(
                rect.width / sprite.texture.width,
                rect.height / sprite.texture.height);
            trailMaterial.mainTextureOffset = new Vector2(
                rect.x / sprite.texture.width,
                rect.y / sprite.texture.height);
        }
        _trail.sharedMaterial = trailMaterial;
        _trail.alignment = LineAlignment.View;
        _trail.textureMode = LineTextureMode.Stretch;
        _trail.minVertexDistance = 0.06f;
        _trail.numCornerVertices = 0;
        _trail.numCapVertices = 0;
        _trail.generateLightingData = false;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.sortingLayerID = _sourceRenderer.sortingLayerID;
        _trail.sortingOrder = _sourceRenderer.sortingOrder - 2;
        _trail.emitting = false;
    }

    private ArcaneSpriteEffect CreateSpriteEffect(string effectName, Sprite[] sprites, int sortingOrder)
    {
        var effectObject = new GameObject(effectName);
        effectObject.transform.SetParent(transform, false);
        var effect = effectObject.AddComponent<ArcaneSpriteEffect>();
        effect.Initialize(sprites, _additiveMaterial, sortingOrder);
        return effect;
    }

    private void OnDestroy()
    {
        if (_additiveMaterial != null) Destroy(_additiveMaterial);
    }
}
