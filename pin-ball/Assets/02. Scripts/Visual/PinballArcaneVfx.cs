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
    private static readonly Color GoldenLow = new(1f, 0.45f, 0.04f, 0.85f);
    private static readonly Color GoldenHigh = new(1f, 0.95f, 0.45f, 1f);
    private static readonly Color GoldenBody = new(1f, 0.72f, 0.12f, 1f);

    private SpriteRenderer _sourceRenderer;
    private Rigidbody2D _body;
    private ArcaneMaskGlowController _glow;
    private TrailRenderer _trail;
    private ArcaneSpriteEffect _impact;
    private ArcaneSpriteEffect _ring;
    private PinballGoldPopup[] _goldPopups;
    private int _nextGoldPopupIndex;
    private Material _additiveMaterial;
    private BattleCameraController _cameraFeedback;
    private bool _initialized;
    private bool _isGolden;
    private Color _originalSourceColor;

    public void Initialize(SpriteRenderer sourceRenderer, Rigidbody2D body)
    {
        if (_initialized) return;

        _sourceRenderer = sourceRenderer;
        _body = body;
        _originalSourceColor = _sourceRenderer.color;

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
        _glow = GetComponent<ArcaneMaskGlowController>();
        _cameraFeedback = Camera.main != null
            ? Camera.main.GetComponent<BattleCameraController>()
            : null;
        CreateTrail(catalog.ballTrail);
        _impact = CreateSpriteEffect("Arcane Impact", catalog.ballImpact, _sourceRenderer.sortingOrder + 3);
        _ring = CreateSpriteEffect("Arcane Ring", catalog.ballRing, _sourceRenderer.sortingOrder + 2);
        _goldPopups = new PinballGoldPopup[4];
        for (var index = 0; index < _goldPopups.Length; index++)
        {
            var popupObject = new GameObject($"Gold Popup {index + 1}");
            _goldPopups[index] = popupObject.AddComponent<PinballGoldPopup>();
            _goldPopups[index].Initialize(
                catalog.goldIcon,
                _sourceRenderer.sortingOrder + 4);
        }
        _initialized = true;
        OnDeactivated();
    }

    public void OnActivated()
    {
        if (!_initialized) return;

        _glow?.SetActiveIntensity(_isGolden ? 2.25f : 1.65f);
        _trail.Clear();
        _trail.emitting = true;
        OnVelocityChanged(_body != null ? _body.linearVelocity : Vector2.zero);
    }

    public void OnDeactivated()
    {
        if (!_initialized) return;

        SetGolden(false);
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

        var hdrColor = _isGolden
            ? Color.Lerp(GoldenLow, GoldenHigh, speed01)
            : Color.Lerp(TrailLow, TrailHigh, speed01);
        _trail.startColor = hdrColor;
        _trail.endColor = new Color(hdrColor.r * 0.2f, hdrColor.g * 0.2f, hdrColor.b * 0.2f, 0f);

        _glow?.SetActiveIntensity(_isGolden
            ? Mathf.Lerp(2f, 2.65f, speed01)
            : Mathf.Lerp(1.35f, 1.9f, speed01));
    }

    public void PlayCollision(Vector2 worldPoint, float relativeSpeed)
    {
        PlayCollision(worldPoint, relativeSpeed, 1f);
    }

    public void PlayCollision(
        Vector2 worldPoint,
        float relativeSpeed,
        float emphasis)
    {
        if (!_initialized || !isActiveAndEnabled) return;

        var strength = Mathf.Clamp01(relativeSpeed / (ReferenceSpeed * 1.25f));
        emphasis = Mathf.Max(0.5f, emphasis);
        var color = _isGolden
            ? Color.Lerp(GoldenLow, GoldenHigh, strength)
            : Color.Lerp(TrailLow, TrailHigh, strength);
        var position = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
        _impact.Play(position, 0.18f + 0.04f * (emphasis - 1f),
            Vector3.one * 0.28f,
            Vector3.one * 0.65f * emphasis,
            color);
        _ring.Play(position, 0.24f + 0.05f * (emphasis - 1f),
            Vector3.one * 0.2f,
            Vector3.one * 0.9f * emphasis,
            color);
        _glow?.Pulse(Mathf.Lerp(1.8f, 2.35f, strength) * emphasis, 0.16f);
        if (emphasis >= 1.25f) _cameraFeedback?.PlayPinballBumperShake();
    }

    public void PlayLoaded()
    {
        if (!_initialized || !isActiveAndEnabled) return;

        _ring.Play(transform.position, 0.26f,
            Vector3.one * 0.3f,
            Vector3.one * 0.8f,
            TrailLow);
        _glow?.Pulse(1.85f, 0.22f);
    }

    public void PlayLaunch()
    {
        if (!_initialized || !isActiveAndEnabled) return;

        _impact.Play(transform.position, 0.16f,
            Vector3.one * 0.35f,
            Vector3.one * 0.85f,
            TrailHigh);
        _ring.Play(transform.position, 0.24f,
            Vector3.one * 0.25f,
            Vector3.one * 1.15f,
            TrailLow);
        _glow?.Pulse(2.5f, 0.2f);
    }

    public void PlayLaunchCamera(float normalizedPull)
    {
        _cameraFeedback?.PlayPinballLaunchShake(normalizedPull);
    }

    public void PlayGoldReward(Vector2 worldPosition, int amount)
    {
        if (!_initialized || _goldPopups == null || amount <= 0) return;

        var popup = _goldPopups[_nextGoldPopupIndex];
        _nextGoldPopupIndex = (_nextGoldPopupIndex + 1) % _goldPopups.Length;
        popup.Play(new Vector3(worldPosition.x, worldPosition.y, transform.position.z), amount);
    }

    public void SetGolden(bool isGolden)
    {
        _isGolden = isGolden;
        if (!_initialized || _sourceRenderer == null) return;

        _sourceRenderer.color = isGolden ? GoldenBody : _originalSourceColor;
        _glow?.SetActiveIntensity(isGolden ? 2.25f : 1.65f);
        if (_trail != null && _trail.emitting)
        {
            OnVelocityChanged(_body != null ? _body.linearVelocity : Vector2.zero);
        }
    }

    public void PlayJackpot(Vector2 worldPosition, int amount)
    {
        if (!_initialized || _goldPopups == null || amount <= 0) return;

        var position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        var popup = _goldPopups[_nextGoldPopupIndex];
        _nextGoldPopupIndex = (_nextGoldPopupIndex + 1) % _goldPopups.Length;
        popup.PlayJackpot(position, amount);
        _impact.Play(position, 0.42f,
            Vector3.one * 0.5f,
            Vector3.one * 1.8f,
            Color.white);
        _ring.Play(position, 0.52f,
            Vector3.one * 0.35f,
            Vector3.one * 2.2f,
            GoldenHigh);
        _glow?.Pulse(4f, 0.42f);
        _cameraFeedback?.PlayPinballGoalShake();
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
        var effect = effectObject.AddComponent<ArcaneSpriteEffect>();
        effect.Initialize(sprites, _additiveMaterial, sortingOrder);
        return effect;
    }

    private void OnDestroy()
    {
        if (_impact != null) Destroy(_impact.gameObject);
        if (_ring != null) Destroy(_ring.gameObject);
        if (_goldPopups != null)
        {
            foreach (var popup in _goldPopups)
            {
                if (popup != null) Destroy(popup.gameObject);
            }
        }
        if (_additiveMaterial != null) Destroy(_additiveMaterial);
    }
}
