using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BaseSkillPanel : MonoBehaviour
{
    private const string SkillName = "전선 밀어내기";

    [SerializeField] private Button useButton;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField, Min(0f)] private float readyFeedbackDuration = 0.35f;

    private BattleManager battleManager;
    private EWaveState lastWaveState;
    private EBaseKnockbackSkillState lastSkillState;
    private int lastRemainingSecond = -1;
    private bool lastInteractable;
    private bool hasDisplayed;
    private bool hasValidReferences;
    private Vector3 buttonBaseScale;

    private void Start()
    {
        hasValidReferences = ValidateReferences();
        if (!hasValidReferences) return;

        buttonBaseScale = useButton.transform.localScale;
        skillNameText.text = SkillName;
        battleManager = App.Get<BattleManager>();
        battleManager.OnStateChanged += OnStateChanged;
        battleManager.OnBaseKnockbackSkillDisplayChanged += Refresh;
        useButton.onClick.AddListener(UseSkill);
        Refresh();
    }

    public static string FormatStatus(
        EWaveState waveState,
        EBaseKnockbackSkillState skillState,
        float remainingTime)
    {
        if (waveState == EWaveState.Pending) return "대기";
        if (skillState == EBaseKnockbackSkillState.Used) return "사용 완료";
        if (waveState != EWaveState.Active) return "대기";
        return skillState == EBaseKnockbackSkillState.Ready
            ? "사용 가능"
            : Mathf.CeilToInt(Mathf.Max(0f, remainingTime)).ToString();
    }

    public static bool ResolveInteractable(
        EWaveState waveState,
        EBaseKnockbackSkillState skillState,
        bool hasAliveEnemy)
    {
        return waveState == EWaveState.Active &&
               skillState == EBaseKnockbackSkillState.Ready &&
               hasAliveEnemy;
    }

    private void OnStateChanged(EWaveState _)
    {
        Refresh();
    }

    private void UseSkill()
    {
        battleManager?.TryUseBaseKnockbackSkill();
    }

    private void Refresh()
    {
        if (battleManager == null || !hasValidReferences) return;

        EWaveState waveState = battleManager.State;
        EBaseKnockbackSkillState skillState =
            battleManager.BaseKnockbackSkillState;
        int remainingSecond = Mathf.CeilToInt(
            battleManager.BaseKnockbackRemainingTime);
        bool interactable = battleManager.CanUseBaseKnockbackSkill;
        bool displayChanged = !hasDisplayed || waveState != lastWaveState ||
                              skillState != lastSkillState ||
                              remainingSecond != lastRemainingSecond;
        bool becameReady = hasDisplayed &&
                           lastSkillState == EBaseKnockbackSkillState.Locked &&
                           skillState == EBaseKnockbackSkillState.Ready;

        if (displayChanged)
        {
            statusText.text = FormatStatus(
                waveState,
                skillState,
                battleManager.BaseKnockbackRemainingTime);
        }

        if (!hasDisplayed || interactable != lastInteractable)
        {
            useButton.interactable = interactable;
        }

        lastWaveState = waveState;
        lastSkillState = skillState;
        lastRemainingSecond = remainingSecond;
        lastInteractable = interactable;
        hasDisplayed = true;
        if (becameReady) PlayReadyFeedback();
    }

    private void PlayReadyFeedback()
    {
        Transform target = useButton.transform;
        target.DOKill();
        target.localScale = buttonBaseScale;
        target.DOPunchScale(
            Vector3.one * 0.18f,
            readyFeedbackDuration,
            7,
            0.5f);
    }

    private bool ValidateReferences()
    {
        bool valid = useButton != null && skillNameText != null &&
                     statusText != null;
        if (!valid)
        {
            Debug.LogError(
                "[BaseSkillPanel] Missing Inspector reference.",
                this);
        }

        return valid;
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnStateChanged -= OnStateChanged;
            battleManager.OnBaseKnockbackSkillDisplayChanged -= Refresh;
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveListener(UseSkill);
            useButton.transform.DOKill();
            if (hasValidReferences)
            {
                useButton.transform.localScale = buttonBaseScale;
            }
        }
    }
}
