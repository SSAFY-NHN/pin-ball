using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameSpeedController : MonoBehaviour
{
    [SerializeField] private Button speedButton;
    [SerializeField] private TextMeshProUGUI speedText;

    private BattleManager _battleManager;
    private int _selectedMultiplier = 1;
    private bool _hasValidReferences;

    private void Start()
    {
        _hasValidReferences = ValidateReferences();
        if (!_hasValidReferences) return;

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        speedButton.onClick.AddListener(ToggleSpeed);
        RefreshSpeed();
    }

    private void ToggleSpeed()
    {
        _selectedMultiplier = _selectedMultiplier == 1 ? 2 : 1;
        RefreshSpeed();
    }

    private void OnBattleStateChanged(EWaveState _)
    {
        ApplyTimeScale();
    }

    private void RefreshSpeed()
    {
        speedText.text = $"{_selectedMultiplier}×";
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        EWaveState state = _battleManager != null
            ? _battleManager.State
            : EWaveState.Pending;
        Time.timeScale = ResolveAppliedTimeScale(
            state,
            _selectedMultiplier);
    }

    public static float ResolveAppliedTimeScale(
        EWaveState state,
        int selectedMultiplier)
    {
        return state == EWaveState.Active && selectedMultiplier == 2
            ? 2f
            : 1f;
    }

    private bool ValidateReferences()
    {
        bool valid = speedButton != null && speedText != null;
        if (!valid)
        {
            Debug.LogError(
                "[GameSpeedController] speedButton and speedText " +
                "must be assigned.");
        }

        return valid;
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
        }

        if (_hasValidReferences)
        {
            speedButton.onClick.RemoveListener(ToggleSpeed);
        }

        Time.timeScale = 1f;
    }
}
