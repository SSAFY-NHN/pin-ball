using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArcaneSpriteEffect : MonoBehaviour
{
    public static float NormalizedLifetime(float elapsed, float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    }

    private SpriteRenderer targetRenderer;
    private Sprite[] frames;
    private float startedAt;
    private float duration;
    private Vector3 worldPosition;
    private Vector3 startScale;
    private Vector3 endScale;
    private Color startColor;
    private bool playing;

    public void Initialize(Sprite[] sprites, Material material, int sortingOrder)
    {
        frames = sprites;
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null) targetRenderer = gameObject.AddComponent<SpriteRenderer>();
        targetRenderer.sharedMaterial = material;
        targetRenderer.sortingOrder = sortingOrder;
        targetRenderer.enabled = false;
    }

    public void Play(
        Vector3 position,
        float lifetime,
        Vector3 initialScale,
        Vector3 finalScale,
        Color color)
    {
        if (frames == null || frames.Length == 0 || targetRenderer == null) return;

        worldPosition = position;
        duration = Mathf.Max(0.01f, lifetime);
        startScale = initialScale;
        endScale = finalScale;
        startColor = color;
        startedAt = Time.time;
        playing = true;
        targetRenderer.sprite = frames[0];
        targetRenderer.color = color;
        targetRenderer.enabled = true;
        transform.position = worldPosition;
        transform.localScale = startScale;
    }

    public void StopEffect()
    {
        playing = false;
        if (targetRenderer != null) targetRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (!playing) return;

        var normalized = NormalizedLifetime(Time.time - startedAt, duration);
        transform.position = worldPosition;
        transform.localScale = Vector3.Lerp(startScale, endScale, normalized);
        targetRenderer.color = new Color(
            startColor.r,
            startColor.g,
            startColor.b,
            startColor.a * (1f - normalized));
        var frameIndex = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(normalized * frames.Length));
        targetRenderer.sprite = frames[frameIndex];

        if (normalized >= 1f) StopEffect();
    }
}
