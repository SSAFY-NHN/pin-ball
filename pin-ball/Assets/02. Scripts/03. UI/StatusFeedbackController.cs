using DG.Tweening;
using TMPro;
using UnityEngine;

internal sealed class StatusFeedbackController
{
    private readonly TextMeshProUGUI _hpText;
    private readonly TextMeshProUGUI _goldText;
    private readonly TextMeshProUGUI _allyCountText;
    private readonly Color _hpFlashColor;
    private readonly Color _goldFlashColor;
    private readonly float _duration;
    private readonly Color _hpBaseColor;
    private readonly Color _goldBaseColor;
    private readonly Vector3 _hpBaseScale;
    private readonly Vector3 _goldBaseScale;

    public StatusFeedbackController(
        TextMeshProUGUI hpText,
        TextMeshProUGUI goldText,
        TextMeshProUGUI allyCountText,
        Color hpFlashColor,
        Color goldFlashColor,
        float duration)
    {
        _hpText = hpText;
        _goldText = goldText;
        _allyCountText = allyCountText;
        _hpFlashColor = hpFlashColor;
        _goldFlashColor = goldFlashColor;
        _duration = duration;
        _hpBaseColor = hpText.color;
        _goldBaseColor = goldText.color;
        _hpBaseScale = hpText.rectTransform.localScale;
        _goldBaseScale = goldText.rectTransform.localScale;
    }

    public void EmphasizeHp()
    {
        PlayResourceFeedback(
            _hpText,
            _hpBaseColor,
            _hpFlashColor,
            _hpBaseScale,
            true);
    }

    public void EmphasizeGold()
    {
        PlayResourceFeedback(
            _goldText,
            _goldBaseColor,
            _goldFlashColor,
            _goldBaseScale,
            false);
    }

    public void EmphasizeAllyCount()
    {
        if (_allyCountText == null) return;

        RectTransform rect = _allyCountText.rectTransform;
        rect.DOKill(true);
        rect.DOShakeAnchorPos(0.3f, 8f, 14, 90f, false, true);
        rect.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.5f);
    }

    public void Clear()
    {
        _hpText?.rectTransform.DOKill();
        _hpText?.DOKill();
        _goldText?.rectTransform.DOKill();
        _goldText?.DOKill();
        _allyCountText?.rectTransform.DOKill();

        if (_hpText != null)
        {
            _hpText.color = _hpBaseColor;
            _hpText.rectTransform.localScale = _hpBaseScale;
        }

        if (_goldText != null)
        {
            _goldText.color = _goldBaseColor;
            _goldText.rectTransform.localScale = _goldBaseScale;
        }
    }

    private void PlayResourceFeedback(
        TextMeshProUGUI text,
        Color baseColor,
        Color flashColor,
        Vector3 baseScale,
        bool shake)
    {
        if (text == null) return;

        RectTransform rect = text.rectTransform;
        rect.DOKill();
        text.DOKill();
        rect.localScale = baseScale;
        text.color = baseColor;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(rect.DOPunchScale(
            Vector3.one * 0.24f,
            _duration,
            8,
            0.55f));
        sequence.Join(text.DOColor(flashColor, 0.1f)
            .SetLoops(2, LoopType.Yoyo));
        if (shake)
        {
            sequence.Join(rect.DOShakeAnchorPos(
                _duration,
                13f,
                18,
                90f,
                false,
                true));
        }

        sequence.OnComplete(() =>
        {
            if (rect != null) rect.localScale = baseScale;
            if (text != null) text.color = baseColor;
        });
    }
}
