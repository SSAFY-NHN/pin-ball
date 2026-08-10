using UnityEngine;

[DisallowMultipleComponent]
public sealed class PinballLauncherGlowController : MonoBehaviour
{
    [SerializeField] private ArcaneMaskGlowController glow;
    [SerializeField, Min(0f)] private float unloadedIntensity = 0.2f;
    [SerializeField, Min(0f)] private float loadedIntensity = 1.15f;
    [SerializeField, Min(0f)] private float hoverIntensity = 1.55f;
    [SerializeField, Min(0f)] private float fullPullIntensity = 2.1f;
    [SerializeField, Min(0f)] private float launchPulseIntensity = 2.6f;
    [SerializeField, Min(0f)] private float breathingAmplitude = 0.12f;
    [SerializeField, Min(0.01f)] private float breathingSpeed = 2.2f;
    [SerializeField, Range(1f, 1.2f)] private float hoverScale = 1.08f;
    [SerializeField, Min(0f)] private float readyPulseIntensity = 1.9f;
    [SerializeField, Min(0f)] private float thresholdPulseIntensity = 2.25f;
    [SerializeField, Min(0f)] private float springIdleIntensity = 0.35f;
    [SerializeField, Min(0f)] private float springFullPullIntensity = 3.4f;

    private bool _loaded;
    private bool _hovered;
    private float _pullRatio;
    private SpriteRenderer _springGlowRenderer;
    private Material _springGlowMaterial;
    private MaterialPropertyBlock _springPropertyBlock;

    private void Update()
    {
        if (glow == null) return;

        float breathing01 =
            (Mathf.Sin(Time.unscaledTime * breathingSpeed) + 1f) * 0.5f;
        float breathingPulse = breathing01 * breathing01 * breathing01;
        glow.SetActiveIntensity(ArcaneGlowMath.CalculateLauncherIntensity(
            _loaded,
            _hovered,
            _pullRatio,
            breathingPulse,
            unloadedIntensity,
            loadedIntensity,
            hoverIntensity,
            fullPullIntensity,
            breathingAmplitude));
        glow.SetScaleMultiplier(
            _loaded && _hovered
                ? Mathf.Lerp(hoverScale, 1.18f, _pullRatio)
                : Mathf.Lerp(1f, 1.18f, _pullRatio));
        UpdateSpringGlow(breathingPulse);
    }

    public void InitializeSpring(SpriteRenderer springRenderer)
    {
        if (springRenderer == null || _springGlowRenderer != null) return;

        var shader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        if (shader == null) return;

        var glowObject = new GameObject("Plunger Spring Glow");
        glowObject.transform.SetParent(springRenderer.transform, false);
        glowObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        _springGlowRenderer = glowObject.AddComponent<SpriteRenderer>();
        _springGlowMaterial = new Material(shader)
        {
            name = "Plunger Spring Glow (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _springGlowMaterial.SetFloat("_GlowSpread", 1.25f);
        _springGlowRenderer.sharedMaterial = _springGlowMaterial;
        _springGlowRenderer.sprite = springRenderer.sprite;
        _springGlowRenderer.sortingLayerID = springRenderer.sortingLayerID;
        _springGlowRenderer.sortingOrder = springRenderer.sortingOrder + 1;
        _springGlowRenderer.color = new Color(0.1f, 0.75f, 1f, 0.9f);
        _springPropertyBlock = new MaterialPropertyBlock();
    }

    public void SetLoaded(bool loaded)
    {
        bool becameLoaded = loaded && !_loaded;
        _loaded = loaded;
        if (!loaded) ResetInteraction();
        else if (becameLoaded) glow?.Pulse(readyPulseIntensity, 0.2f);
    }

    public void SetHovered(bool hovered) => _hovered = hovered;

    public void SetPullRatio(float pullRatio)
    {
        _pullRatio = Mathf.Clamp01(pullRatio);
    }

    public void PlayLaunch()
    {
        glow?.Pulse(launchPulseIntensity, 0.24f);
    }

    public void PlayThresholdReached()
    {
        glow?.Pulse(thresholdPulseIntensity, 0.12f);
    }

    public void ResetInteraction()
    {
        _hovered = false;
        _pullRatio = 0f;
        glow?.SetScaleMultiplier(1f);
    }

    private void UpdateSpringGlow(float breathingPulse)
    {
        if (_springGlowRenderer == null || _springPropertyBlock == null) return;

        float loadedIntensity = _loaded
            ? springIdleIntensity + breathingPulse * 0.2f
            : 0f;
        float intensity = Mathf.Lerp(
            loadedIntensity,
            springFullPullIntensity,
            _pullRatio);
        _springPropertyBlock.Clear();
        _springPropertyBlock.SetFloat("_Intensity", intensity);
        _springGlowRenderer.SetPropertyBlock(_springPropertyBlock);
        _springGlowRenderer.enabled = intensity > 0.01f;
    }

    private void OnDestroy()
    {
        if (_springGlowMaterial != null) Destroy(_springGlowMaterial);
    }
}
