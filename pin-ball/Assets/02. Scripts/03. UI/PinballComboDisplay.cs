using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class PinballComboDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform textGroup;
    [SerializeField] private TextMeshProUGUI backgroundText;
    [SerializeField] private TextMeshProUGUI foregroundText;
    [SerializeField] private RectTransform fillMask;

    private PinballManager _pinballManager;
    private Vector3 _baseScale;
    private float _fullMaskWidth;
    private bool _hasValidReferences;

    private void Start()
    {
        _hasValidReferences = ValidateReferences();
        if (!_hasValidReferences) return;

        _baseScale = textGroup.localScale;
        _fullMaskWidth = fillMask.rect.width;
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.OnComboChanged += OnComboChanged;
        OnComboChanged(_pinballManager.CurrentCombo);
    }

    private void Update()
    {
        if (!_hasValidReferences ||
            _pinballManager == null ||
            _pinballManager.CurrentCombo <= 0)
        {
            return;
        }

        SetFill(_pinballManager.CurrentComboProgress);
    }

    private void OnComboChanged(int combo)
    {
        if (!_hasValidReferences) return;

        bool visible = combo > 0;
        textGroup.gameObject.SetActive(visible);
        if (!visible) return;

        string comboText =
            $"{combo} COMBO x{_pinballManager.CurrentComboMultiplier:0.#}";
        backgroundText.text = comboText;
        foregroundText.text = comboText;
        SetFill(1f);
        PlayComboPunch();
    }

    private void SetFill(float progress)
    {
        fillMask.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            _fullMaskWidth * Mathf.Clamp01(progress));
    }

    private void PlayComboPunch()
    {
        textGroup.DOKill();
        textGroup.localScale = _baseScale;
        textGroup.DOPunchScale(
                new Vector3(0.18f, 0.18f, 0f),
                0.22f,
                5,
                0.5f)
            .SetUpdate(true);
    }

    private bool ValidateReferences()
    {
        bool valid =
            textGroup != null &&
            backgroundText != null &&
            foregroundText != null &&
            fillMask != null;
        if (!valid)
        {
            Debug.LogError(
                "[PinballComboDisplay] textGroup, backgroundText, " +
                "foregroundText, and fillMask must be assigned.");
        }

        return valid;
    }

    private void OnDisable()
    {
        if (!_hasValidReferences) return;

        textGroup.DOKill();
        textGroup.localScale = _baseScale;
    }

    private void OnDestroy()
    {
        if (_pinballManager != null)
        {
            _pinballManager.OnComboChanged -= OnComboChanged;
        }
    }
}
