using TMPro;

using UnityEngine;

public sealed class BattleDamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.55f;

    private TextMeshPro _text;
    private Vector3 _origin;
    private Color _color;
    private float _startedAt;

    public void Initialize(int sortingOrder)
    {
        _text = gameObject.AddComponent<TextMeshPro>();
        _text.font = TMP_Settings.defaultFontAsset;
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontSize = 24f;
        _text.fontStyle = FontStyles.Bold;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.rectTransform.sizeDelta = new Vector2(3f, 1.2f);
        _text.sortingOrder = sortingOrder;
        transform.localScale = Vector3.one * 0.1f;
        gameObject.SetActive(false);
    }

    public void Play(Vector3 position, float damage, EBattleTeam damagedTeam)
    {
        Color color = damagedTeam == EBattleTeam.Ally
            ? new Color(1f, 0.2f, 0.2f, 1f)
            : new Color(1f, 0.72f, 0.18f, 1f);
        PlayValue(position, Mathf.CeilToInt(damage).ToString(), color);
    }

    public void PlayHeal(Vector3 position, float amount)
    {
        PlayValue(
            position,
            $"+{Mathf.CeilToInt(amount)}",
            new Color(0.25f, 1f, 0.42f, 1f));
    }

    private void PlayValue(Vector3 position, string value, Color color)
    {
        _origin = position + Vector3.up * 0.35f;
        _startedAt = Time.unscaledTime;
        _color = color;
        gameObject.SetActive(true);
        _text.text = value;
        _text.color = _color;
        _text.ForceMeshUpdate(true);
        transform.position = _origin;
    }

    private void Update()
    {
        float progress = Mathf.Clamp01((Time.unscaledTime - _startedAt) / Lifetime);
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        transform.position = _origin + Vector3.up * (0.55f * eased);
        float alpha = 1f - Mathf.Clamp01((progress - 0.5f) * 2f);
        _text.color = new Color(_color.r, _color.g, _color.b, alpha);
        if (progress >= 1f) gameObject.SetActive(false);
    }
}
