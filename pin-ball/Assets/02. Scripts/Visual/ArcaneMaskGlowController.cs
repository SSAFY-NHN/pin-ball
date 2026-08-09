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
}

[DisallowMultipleComponent]
public sealed class ArcaneMaskGlowController : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly Color DefaultGlowColor = new(0.03f, 0.55f, 1f, 0.9f);

    [SerializeField, Min(0f)] private float baseIntensity = 1.65f;

    public float CurrentIntensity { get; private set; }

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer glowRenderer;
    private Material glowMaterial;
    private MaterialPropertyBlock propertyBlock;
    private float pulseIntensity;
    private float pulseDuration;
    private float pulseStartedAt = float.NegativeInfinity;

    public static ArcaneMaskGlowController Attach(SpriteRenderer source, Sprite mask)
    {
        if (source == null || mask == null) return null;
        var controller = source.GetComponent<ArcaneMaskGlowController>();
        if (controller == null) controller = source.gameObject.AddComponent<ArcaneMaskGlowController>();
        controller.Initialize(source, mask);
        return controller;
    }

    public void Initialize(SpriteRenderer source, Sprite mask)
    {
        sourceRenderer = source;
        if (sourceRenderer == null || sourceRenderer.sprite == null || mask == null) return;

        if (glowRenderer == null)
        {
            var child = new GameObject("Arcane Mask Glow");
            child.transform.SetParent(transform, false);
            glowRenderer = child.AddComponent<SpriteRenderer>();
        }

        var shader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        if (shader == null)
        {
            glowRenderer.enabled = false;
            Debug.LogWarning("Arcane additive shader was not found.", this);
            return;
        }

        if (glowMaterial == null)
        {
            glowMaterial = new Material(shader)
            {
                name = "Arcane Mask Glow (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        propertyBlock ??= new MaterialPropertyBlock();
        glowRenderer.sharedMaterial = glowMaterial;
        glowRenderer.sprite = mask;
        glowRenderer.color = DefaultGlowColor;
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
            glowRenderer.sprite.bounds.size);
        glowRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var scaledMaskCenter = Vector2.Scale(glowRenderer.sprite.bounds.center, scale);
        var offset = (Vector2)sourceRenderer.sprite.bounds.center - scaledMaskCenter;
        glowRenderer.transform.localPosition = new Vector3(offset.x, offset.y, -0.01f);
    }

    private void SetIntensity(float intensity)
    {
        CurrentIntensity = intensity;
        propertyBlock.Clear();
        propertyBlock.SetFloat(IntensityId, intensity);
        glowRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDestroy()
    {
        if (glowMaterial != null) Destroy(glowMaterial);
    }
}
