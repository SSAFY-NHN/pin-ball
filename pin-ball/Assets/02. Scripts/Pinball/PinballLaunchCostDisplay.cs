using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PinballLaunchCostDisplay : MonoBehaviour
{
    private static readonly Color AvailableColor = new(1f, 0.82f, 0.24f, 1f);
    private static readonly Color UnavailableColor = new(1f, 0.32f, 0.38f, 1f);

    [SerializeField] private TextMeshPro costText;

    private PinballManager _pinballManager;
    private BattleManager _battleManager;

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
        _battleManager = App.Get<BattleManager>();
        if (costText == null)
        {
            Debug.LogError("[PinballLaunchCostDisplay] Launch cost text is not assigned.");
            enabled = false;
            return;
        }

        _pinballManager.OnLaunchCostChanged += Refresh;
        _battleManager.OnGoldChanged += OnGoldChanged;
        Refresh(_pinballManager.CurrentLaunchCost);
    }

    private void OnGoldChanged(int _)
    {
        Refresh(_pinballManager.CurrentLaunchCost);
    }

    private void Refresh(int launchCost)
    {
        if (costText == null || _battleManager == null) return;

        costText.text = $"{launchCost}G";
        costText.color = _battleManager.Gold >= launchCost
            ? AvailableColor
            : UnavailableColor;
    }

    private void OnDestroy()
    {
        if (_pinballManager != null)
        {
            _pinballManager.OnLaunchCostChanged -= Refresh;
        }

        if (_battleManager != null)
        {
            _battleManager.OnGoldChanged -= OnGoldChanged;
        }
    }
}
