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

    private bool _loaded;
    private bool _hovered;
    private float _pullRatio;

    private void Update()
    {
        if (glow == null) return;

        float breathing01 =
            (Mathf.Sin(Time.unscaledTime * breathingSpeed) + 1f) * 0.5f;
        glow.SetActiveIntensity(ArcaneGlowMath.CalculateLauncherIntensity(
            _loaded,
            _hovered,
            _pullRatio,
            breathing01,
            unloadedIntensity,
            loadedIntensity,
            hoverIntensity,
            fullPullIntensity,
            breathingAmplitude));
        glow.SetScaleMultiplier(
            _loaded && _hovered
                ? Mathf.Lerp(hoverScale, 1.12f, _pullRatio)
                : Mathf.Lerp(1f, 1.12f, _pullRatio));
    }

    public void SetLoaded(bool loaded)
    {
        _loaded = loaded;
        if (!loaded) ResetInteraction();
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

    public void ResetInteraction()
    {
        _hovered = false;
        _pullRatio = 0f;
        glow?.SetScaleMultiplier(1f);
    }
}
