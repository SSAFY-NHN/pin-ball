using UnityEngine;

public static class ArcaneGlowMath
{
    public static Vector2 CalculateMaskScale(Vector2 sourceSize, Vector2 maskSize)
    {
        return new Vector2(
            maskSize.x > 0.001f ? sourceSize.x / maskSize.x : 1f,
            maskSize.y > 0.001f ? sourceSize.y / maskSize.y : 1f);
    }

    public static float EvaluatePulse(
        float baseIntensity,
        float pulseIntensity,
        float pulseDuration,
        float elapsed)
    {
        if (pulseDuration <= 0f || elapsed >= pulseDuration) return baseIntensity;
        return Mathf.Lerp(pulseIntensity, baseIntensity, elapsed / pulseDuration);
    }

    public static float CalculateLauncherIntensity(
        bool loaded,
        bool hovered,
        float pullRatio,
        float breathing01,
        float unloadedIntensity,
        float loadedIntensity,
        float hoverIntensity,
        float fullPullIntensity,
        float breathingAmplitude)
    {
        if (!loaded) return Mathf.Max(0f, unloadedIntensity);

        float idle = hovered ? hoverIntensity : loadedIntensity;
        idle += Mathf.Clamp01(breathing01) *
                Mathf.Max(0f, breathingAmplitude);
        return Mathf.Lerp(
            Mathf.Max(0f, idle),
            Mathf.Max(0f, fullPullIntensity),
            Mathf.Clamp01(pullRatio));
    }
}

[DisallowMultipleComponent]
public sealed class ArcaneMaskGlowController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField, Min(0f)] private float baseIntensity = 1.65f;

    public float CurrentIntensity { get; private set; }

    private MaterialPropertyBlock propertyBlock;
    private float pulseIntensity;
    private float pulseDuration;
    private float pulseStartedAt = float.NegativeInfinity;
    private float scaleMultiplier = 1f;

    private void Awake()
    {
        if (sourceRenderer == null || glowRenderer == null)
        {
            Debug.LogError(
                "[ArcaneMaskGlowController] Source and glow renderers must be serialized.",
                this);
            enabled = false;
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        SyncToSource();
        SetIntensity(baseIntensity);
    }

    public void Pulse(float intensity = 2f, float duration = 0.22f)
    {
        pulseIntensity = intensity;
        pulseDuration = duration;
        pulseStartedAt = Time.time;
        SetIntensity(intensity);
    }

    public void SetActiveIntensity(float intensity)
    {
        baseIntensity = intensity;
        if (Time.time - pulseStartedAt >= pulseDuration) SetIntensity(baseIntensity);
    }

    public void SetScaleMultiplier(float multiplier)
    {
        scaleMultiplier = Mathf.Max(0.01f, multiplier);
    }

    private void LateUpdate()
    {
        if (glowRenderer == null) return;
        SyncToSource();
        SetIntensity(ArcaneGlowMath.EvaluatePulse(
            baseIntensity,
            pulseIntensity,
            pulseDuration,
            Time.time - pulseStartedAt));
    }

    private void SyncToSource()
    {
        glowRenderer.flipX = sourceRenderer.flipX;
        glowRenderer.flipY = sourceRenderer.flipY;
        glowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        glowRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;

        var scale = ArcaneGlowMath.CalculateMaskScale(
            sourceRenderer.sprite.bounds.size,
            glowRenderer.sprite.bounds.size) * scaleMultiplier;
        glowRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var scaledMaskCenter = Vector2.Scale(glowRenderer.sprite.bounds.center, scale);
        var offset = (Vector2)sourceRenderer.sprite.bounds.center - scaledMaskCenter;
        glowRenderer.transform.localPosition = new Vector3(offset.x, offset.y, -0.01f);
    }

    private void SetIntensity(float intensity)
    {
        CurrentIntensity = intensity;
        propertyBlock.Clear();
        propertyBlock.SetFloat("_Intensity", intensity);
        glowRenderer.SetPropertyBlock(propertyBlock);
    }
}
