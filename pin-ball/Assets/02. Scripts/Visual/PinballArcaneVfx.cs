using UnityEngine;

/// <summary>
/// Lightweight, self-contained feedback for pooled pinballs. All renderers,
/// materials, and particle storage are created once and reused.
/// </summary>
[DisallowMultipleComponent]
public sealed class PinballArcaneVfx : MonoBehaviour
{
    private const float ReferenceSpeed = 8f;
    private const int ImpactParticleCount = 10;

    private static readonly Color TrailLow = new(0.1f, 0.75f, 1.8f, 0.72f);
    private static readonly Color TrailHigh = new(1.2f, 0.18f, 2.4f, 0.92f);
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

    private SpriteRenderer _sourceRenderer;
    private Rigidbody2D _body;
    private SpriteRenderer _glowRenderer;
    private TrailRenderer _trail;
    private ParticleSystem _impact;
    private Material _spriteMaterial;
    private Material _additiveMaterial;
    private MaterialPropertyBlock _propertyBlock;
    private ParticleSystem.Particle[] _impactParticles;
    private bool _initialized;

    public void Initialize(SpriteRenderer sourceRenderer, Rigidbody2D body)
    {
        if (_initialized) return;

        _sourceRenderer = sourceRenderer;
        _body = body;

        var spriteShader = Resources.Load<Shader>("ArcaneVFX/ArcaneSprite");
        var additiveShader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        if (spriteShader == null || additiveShader == null)
        {
            Debug.LogWarning("Arcane pinball shaders were not found in Resources.", this);
            enabled = false;
            return;
        }

        _spriteMaterial = new Material(spriteShader)
        {
            name = "Arcane Sprite (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _additiveMaterial = new Material(additiveShader)
        {
            name = "Arcane Additive (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _propertyBlock = new MaterialPropertyBlock();
        _impactParticles = new ParticleSystem.Particle[ImpactParticleCount];

        CreateGlow();
        CreateTrail();
        CreateImpactParticles();
        _initialized = true;
        OnDeactivated();
    }

    public void OnActivated()
    {
        if (!_initialized) return;

        SyncGlowSprite();
        _glowRenderer.enabled = true;
        _trail.Clear();
        _trail.emitting = true;
        OnVelocityChanged(_body != null ? _body.linearVelocity : Vector2.zero);
    }

    public void OnDeactivated()
    {
        if (!_initialized) return;

        _trail.emitting = false;
        _trail.Clear();
        _glowRenderer.enabled = false;
        _impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void OnVelocityChanged(Vector2 velocity)
    {
        if (!_initialized || !_trail.emitting) return;

        SyncGlowSprite();
        var speed01 = Mathf.Clamp01(velocity.magnitude / (ReferenceSpeed * 1.5f));
        _trail.time = Mathf.Lerp(0.08f, 0.2f, speed01);
        _trail.widthMultiplier = Mathf.Lerp(0.14f, 0.24f, speed01);

        var hdrColor = Color.Lerp(TrailLow, TrailHigh, speed01);
        _trail.startColor = hdrColor;
        _trail.endColor = new Color(hdrColor.r * 0.2f, hdrColor.g * 0.2f, hdrColor.b * 0.2f, 0f);

        _propertyBlock.Clear();
        _propertyBlock.SetColor(EmissionColorId, hdrColor);
        _propertyBlock.SetFloat(EmissionStrengthId, Mathf.Lerp(1.15f, 1.8f, speed01));
        _glowRenderer.SetPropertyBlock(_propertyBlock);
        _glowRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.18f, speed01);
    }

    public void PlayCollision(Vector2 worldPoint, float relativeSpeed)
    {
        if (!_initialized || !isActiveAndEnabled) return;

        _impact.transform.position = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
        var strength = Mathf.Clamp01(relativeSpeed / (ReferenceSpeed * 1.25f));
        var color = Color.Lerp(TrailLow, TrailHigh, strength);
        var lifetime = Mathf.Lerp(0.12f, 0.22f, strength);
        var particleSpeed = Mathf.Lerp(1.2f, 3.2f, strength);

        for (var i = 0; i < _impactParticles.Length; i++)
        {
            var angle = (Mathf.PI * 2f * i / _impactParticles.Length) + strength * 0.35f;
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            _impactParticles[i] = new ParticleSystem.Particle
            {
                position = _impact.transform.position,
                velocity = direction * particleSpeed * Mathf.Lerp(0.72f, 1f, i / 9f),
                startLifetime = lifetime,
                remainingLifetime = lifetime,
                startSize = Mathf.Lerp(0.05f, 0.12f, strength),
                startColor = color,
                rotation = angle * Mathf.Rad2Deg
            };
        }

        _impact.SetParticles(_impactParticles, _impactParticles.Length);
        _impact.Play();
    }

    private void CreateGlow()
    {
        var glowObject = new GameObject("Arcane Glow");
        glowObject.transform.SetParent(transform, false);
        _glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        _glowRenderer.sharedMaterial = _spriteMaterial;
        _glowRenderer.maskInteraction = _sourceRenderer.maskInteraction;
        SyncGlowSprite();
    }

    private void CreateTrail()
    {
        _trail = gameObject.AddComponent<TrailRenderer>();
        _trail.sharedMaterial = _additiveMaterial;
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

    private void CreateImpactParticles()
    {
        var impactObject = new GameObject("Arcane Impact");
        impactObject.transform.SetParent(transform, false);
        _impact = impactObject.AddComponent<ParticleSystem>();
        _impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _impact.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = ImpactParticleCount;
        main.startSpeed = 0f;
        main.startLifetime = 0.18f;
        main.startSize = 0.08f;
        main.gravityModifier = 0f;

        var emission = _impact.emission;
        emission.enabled = false;
        var shape = _impact.shape;
        shape.enabled = false;

        var renderer = _impact.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = _additiveMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerID = _sourceRenderer.sortingLayerID;
        renderer.sortingOrder = _sourceRenderer.sortingOrder + 3;
    }

    private void SyncGlowSprite()
    {
        if (_sourceRenderer == null || _glowRenderer == null) return;

        _glowRenderer.sprite = _sourceRenderer.sprite;
        _glowRenderer.flipX = _sourceRenderer.flipX;
        _glowRenderer.flipY = _sourceRenderer.flipY;
        _glowRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
        _glowRenderer.sortingOrder = _sourceRenderer.sortingOrder - 1;
    }

    private void OnDestroy()
    {
        if (_spriteMaterial != null) Destroy(_spriteMaterial);
        if (_additiveMaterial != null) Destroy(_additiveMaterial);
    }
}
