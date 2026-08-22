using System.Collections;

using UnityEngine;

public class GameLayoutController : UIBase
{
    public override bool IsDefaultPanel => true;

    [SerializeField] private RectTransform pinballPanel;
    [SerializeField] private CanvasGroup pinballCanvasGroup;
    [SerializeField] private CanvasGroup pinballCoverCanvasGroup;
    [SerializeField] private Vector2 pinballVisiblePosition;
    [SerializeField] private Vector2 pinballHiddenPosition;
    [SerializeField, Min(0f)] private float slideDuration = 0.25f;

    private BattleManager _battleManager;
    private Coroutine _animation;
    private bool _isPinballVisible;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        if (!ValidateReferences()) return;

        _battleManager = App.Get<BattleManager>();
        SetPinballVisible(true, true);
    }

    public void SetPinballVisible(bool visible, bool immediate)
    {
        if (pinballPanel == null || pinballCanvasGroup == null) return;

        _isPinballVisible = visible;
        Vector2 targetPosition = visible
            ? pinballVisiblePosition
            : pinballHiddenPosition;
        float targetAlpha = visible ? 1f : 0f;
        float targetCoverAlpha = visible ? 0f : 1f;

        if (_animation != null)
        {
            StopCoroutine(_animation);
            _animation = null;
        }

        pinballCanvasGroup.blocksRaycasts = visible;
        pinballCanvasGroup.interactable = visible;
        if (immediate || slideDuration <= 0f)
        {
            pinballPanel.anchoredPosition = targetPosition;
            pinballCanvasGroup.alpha = targetAlpha;
            pinballCoverCanvasGroup.alpha = targetCoverAlpha;
            return;
        }

        _animation = StartCoroutine(AnimatePinball(
            targetPosition,
            targetAlpha,
            targetCoverAlpha));
    }

    private IEnumerator AnimatePinball(
        Vector2 targetPosition,
        float targetAlpha,
        float targetCoverAlpha)
    {
        Vector2 startPosition = pinballPanel.anchoredPosition;
        float startAlpha = pinballCanvasGroup.alpha;
        float startCoverAlpha = pinballCoverCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / slideDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            pinballPanel.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                eased);
            pinballCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                eased);
            pinballCoverCanvasGroup.alpha = Mathf.Lerp(
                startCoverAlpha,
                targetCoverAlpha,
                eased);
            yield return null;
        }

        pinballPanel.anchoredPosition = targetPosition;
        pinballCanvasGroup.alpha = targetAlpha;
        pinballCoverCanvasGroup.alpha = targetCoverAlpha;
        pinballCanvasGroup.blocksRaycasts = _isPinballVisible;
        pinballCanvasGroup.interactable = _isPinballVisible;
        _animation = null;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (pinballPanel == null)
        {
            Debug.LogError(
                "[GameLayoutController] Missing reference: pinballPanel");
            isValid = false;
        }

        if (pinballCanvasGroup == null)
        {
            Debug.LogError(
                "[GameLayoutController] Missing reference: pinballCanvasGroup");
            isValid = false;
        }

        if (pinballCoverCanvasGroup == null)
        {
            Debug.LogError(
                "[GameLayoutController] Missing reference: " +
                "pinballCoverCanvasGroup");
            isValid = false;
        }

        return isValid;
    }

    private void OnDestroy()
    {
    }
}
