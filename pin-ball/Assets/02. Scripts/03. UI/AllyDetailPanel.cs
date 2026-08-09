using System.Collections;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AllyDetailPanel : UIBase
{
    public override bool IsDefaultPanel => true;

    [Header("Animation")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector2 hiddenPosition;
    [SerializeField] private Vector2 visiblePosition;
    [SerializeField, Min(0f)] private float showDuration = 0.2f;
    [SerializeField, Min(0f)] private float hideDuration = 0.15f;

    [Header("Input")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button emptyAreaButton;

    [Header("Unit")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailText;

    private UnitManager _unitManager;
    private TitleData _titleData;
    private Coroutine _animation;
    private bool _isVisible;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        if (!ValidateReferences()) return;

        _unitManager = App.Get<UnitManager>();
        _titleData = App.Get<TitleData>();
        _unitManager.OnAllyDetailRequested += Show;
        closeButton.onClick.AddListener(HideDetail);
        emptyAreaButton.onClick.AddListener(HideDetail);

        panelRect.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        emptyAreaButton.gameObject.SetActive(false);
    }

    public void Show(AllyUnit ally)
    {
        if (ally == null || panelRect == null) return;

        RefreshContent(ally);
        _isVisible = true;
        emptyAreaButton.gameObject.SetActive(true);
        StartAnimation(visiblePosition, 1f, showDuration);
    }

    public void HideDetail()
    {
        if (!_isVisible || panelRect == null) return;

        _isVisible = false;
        emptyAreaButton.gameObject.SetActive(false);
        StartAnimation(hiddenPosition, 0f, hideDuration);
    }

    private void RefreshContent(AllyUnit ally)
    {
        AllyUnitData data = null;
        if (_titleData == null ||
            !_titleData.TryGetAllyUnit(ally.UnitId, out data))
        {
            Debug.LogError(
                $"[AllyDetailPanel] Ally data not found: {ally.UnitId}");
            titleText.text = $"{ally.UnitId}  Lv. {ally.Level}";
        }
        else
        {
            titleText.text = $"{data.name}  Lv. {ally.Level}";
        }

        string role = data == null ? "-" : data.role;
        AllySkillData skill = ally.Skill;
        string skillName = skill == null ? "스킬 정보 없음" : skill.name;
        string skillDescription = skill == null
            ? "등록된 스킬 설명이 없습니다."
            : skill.description;
        detailText.text =
            $"역할  {role}\n\n" +
            $"HP  {ally.MaxHp:0}\n" +
            $"공격력  {ally.AttackDamage:0}\n" +
            $"방어력  {ally.CurrentDefense:0}\n" +
            $"공격 속도  {ally.AttackRate:0.##}\n" +
            $"사거리  {ally.AttackRange:0.##}\n" +
            $"마나  {ally.CurrentMana:0}/{ally.MaxMana:0}\n\n" +
            $"<b>{skillName}</b>\n{skillDescription}";
    }

    private void StartAnimation(
        Vector2 targetPosition,
        float targetAlpha,
        float duration)
    {
        if (_animation != null)
        {
            StopCoroutine(_animation);
        }

        _animation = StartCoroutine(Animate(
            targetPosition,
            targetAlpha,
            duration));
    }

    private IEnumerator Animate(
        Vector2 targetPosition,
        float targetAlpha,
        float duration)
    {
        Vector2 startPosition = panelRect.anchoredPosition;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        canvasGroup.blocksRaycasts = _isVisible;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = duration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            panelRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                eased);
            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                eased);
            yield return null;
        }

        panelRect.anchoredPosition = targetPosition;
        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = _isVisible;
        canvasGroup.blocksRaycasts = _isVisible;
        _animation = null;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;
        isValid &= ValidateReference(panelRect, nameof(panelRect));
        isValid &= ValidateReference(canvasGroup, nameof(canvasGroup));
        isValid &= ValidateReference(closeButton, nameof(closeButton));
        isValid &= ValidateReference(emptyAreaButton, nameof(emptyAreaButton));
        isValid &= ValidateReference(titleText, nameof(titleText));
        isValid &= ValidateReference(detailText, nameof(detailText));
        return isValid;
    }

    private bool ValidateReference(Object reference, string fieldName)
    {
        if (reference != null) return true;
        Debug.LogError($"[AllyDetailPanel] Missing reference: {fieldName}");
        return false;
    }

    private void OnDestroy()
    {
        if (_unitManager != null)
        {
            _unitManager.OnAllyDetailRequested -= Show;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideDetail);
        }

        if (emptyAreaButton != null)
        {
            emptyAreaButton.onClick.RemoveListener(HideDetail);
        }
    }
}
