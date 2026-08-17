using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class WaveResultPanel : UIBase
{
    public override bool IsManagedByStack => false;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private string clearedCopy = "다음 단계 준비 중";
    [SerializeField] private string failedCopy = "같은 단계 재정비";
    [SerializeField] private Color clearedColor =
        new(1f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color failedColor =
        new(1f, 0.28f, 0.24f, 1f);

    private BattleManager _battleManager;
    private Sequence _sequence;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        if (panelRect == null || canvasGroup == null || resultText == null)
        {
            Debug.LogError(
                "[WaveResultPanel] Missing serialized UI reference.");
            enabled = false;
            return;
        }

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnWaveResolutionStarted += OnWaveResolutionStarted;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelRect.localScale = Vector3.one;
    }

    public static string ResolveCopy(
        EWaveResolutionResult result,
        string clearedCopy,
        string failedCopy)
    {
        return result == EWaveResolutionResult.Cleared
            ? clearedCopy
            : failedCopy;
    }

    private void OnWaveResolutionStarted(
        EWaveResolutionResult result,
        int _)
    {
        _sequence?.Kill();
        transform.SetAsLastSibling();
        resultText.text = ResolveCopy(result, clearedCopy, failedCopy);
        resultText.color = result == EWaveResolutionResult.Cleared
            ? clearedColor
            : failedColor;
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.78f;

        _sequence = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, 0.15f))
            .Join(panelRect.DOScale(1f, 0.22f)
                .SetEase(Ease.OutBack))
            .AppendInterval(1.53f)
            .Append(canvasGroup.DOFade(0f, 0.2f))
            .OnComplete(() =>
            {
                panelRect.localScale = Vector3.one;
                _sequence = null;
            });
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        if (_battleManager != null)
        {
            _battleManager.OnWaveResolutionStarted -=
                OnWaveResolutionStarted;
        }
    }
}
