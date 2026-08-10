using UnityEngine;

[DisallowMultipleComponent]
public sealed class AllyDragLineageHighlight : MonoBehaviour
{
    private static readonly Color Purple = new(0.68f, 0.45f, 1f, 1f);
    private static readonly Color Gold = new(1f, 0.86f, 0.35f, 1f);

    private SpriteRenderer _renderer;
    private Vector3 _baseScale;
    private bool _isHighlighted;

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (_isHighlighted == highlighted) return;

        _isHighlighted = highlighted;
        if (highlighted)
        {
            _baseScale = transform.localScale;
            return;
        }

        transform.localScale = _baseScale;
    }

    private void LateUpdate()
    {
        if (!_isHighlighted || _renderer == null) return;

        float wave = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
        Color glowColor = Color.Lerp(Purple, Gold, wave);
        _renderer.color = Color.Lerp(
            Color.white,
            glowColor,
            0.45f + wave * 0.35f);
        transform.localScale = _baseScale * Mathf.Lerp(1.03f, 1.1f, wave);
    }

    private void OnDisable()
    {
        _isHighlighted = false;
        transform.localScale = _baseScale;
    }
}
