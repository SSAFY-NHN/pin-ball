using TMPro;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class PinballGoldPopup : MonoBehaviour
{
    private const float Lifetime = 0.65f;
    private static readonly Color GoldColor = new(1f, 0.75f, 0.12f, 1f);

    public bool IsPlaying { get; private set; }

    private SpriteRenderer _iconRenderer;
    private TextMeshPro _amountText;
    private Vector3 _startPosition;
    private float _startedAt;

    public void Initialize(Sprite goldIcon, int sortingOrder)
    {
        var iconObject = new GameObject("Gold Icon");
        iconObject.transform.SetParent(transform, false);
        iconObject.transform.localPosition = new Vector3(-0.22f, 0f, 0f);
        iconObject.transform.localScale = Vector3.one * 0.32f;
        _iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        _iconRenderer.sprite = goldIcon;
        _iconRenderer.sortingOrder = sortingOrder;

        var textObject = new GameObject("Gold Amount");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0.18f, 0f, 0f);
        textObject.transform.localScale = Vector3.one * 0.12f;
        _amountText = textObject.AddComponent<TextMeshPro>();
        _amountText.font = TMP_Settings.defaultFontAsset;
        _amountText.alignment = TextAlignmentOptions.MidlineLeft;
        _amountText.fontSize = 30f;
        _amountText.fontStyle = FontStyles.Bold;
        _amountText.overflowMode = TextOverflowModes.Overflow;
        _amountText.rectTransform.sizeDelta = new Vector2(5f, 1.2f);
        _amountText.color = GoldColor;
        _amountText.sortingOrder = sortingOrder;
        _amountText.renderer.sortingLayerID = _iconRenderer.sortingLayerID;
        _amountText.renderer.sortingOrder = sortingOrder;
        gameObject.SetActive(false);
    }

    public void Play(Vector3 worldPosition, int amount)
    {
        if (_iconRenderer == null || _amountText == null || amount <= 0) return;

        _startPosition = worldPosition;
        _startedAt = Time.unscaledTime;
        gameObject.SetActive(true);
        _amountText.text = $"+{amount}";
        _iconRenderer.color = Color.white;
        _amountText.color = GoldColor;
        _amountText.ForceMeshUpdate(true);
        transform.position = worldPosition;
        transform.localScale = Vector3.one * 0.8f;
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;

        float progress = Mathf.Clamp01((Time.unscaledTime - _startedAt) / Lifetime);
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        transform.position = _startPosition + Vector3.up * (0.8f * eased);
        transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, Mathf.Min(1f, progress * 4f));
        float alpha = 1f - Mathf.Clamp01((progress - 0.45f) / 0.55f);
        _iconRenderer.color = new Color(1f, 1f, 1f, alpha);
        _amountText.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, alpha);

        if (progress < 1f) return;

        IsPlaying = false;
        gameObject.SetActive(false);
    }
}
