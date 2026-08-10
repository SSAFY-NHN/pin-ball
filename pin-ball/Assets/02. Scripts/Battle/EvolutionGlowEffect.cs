using System.Collections;

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EvolutionGlowEffect : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float duration = 0.75f;
    [SerializeField, Min(0.01f)] private float startScale = 0.8f;
    [SerializeField, Min(0.01f)] private float endScale = 2f;

    private SpriteRenderer _spriteRenderer;
    private Coroutine _animation;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        gameObject.SetActive(true);

        if (_animation != null)
        {
            StopCoroutine(_animation);
        }

        _animation = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        Color baseColor = _spriteRenderer.color;

        while (elapsed < duration)
        {
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(startScale, endScale, normalizedTime);
            float alpha = Mathf.Sin(normalizedTime * Mathf.PI);

            transform.localScale = new Vector3(scale, scale, 1f);
            _spriteRenderer.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = new Vector3(endScale, endScale, 1f);
        _spriteRenderer.color = new Color(
            baseColor.r,
            baseColor.g,
            baseColor.b,
            0f);
        _animation = null;
        gameObject.SetActive(false);
    }
}
