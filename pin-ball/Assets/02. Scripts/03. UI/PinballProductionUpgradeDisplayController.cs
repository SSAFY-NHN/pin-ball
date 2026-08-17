using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PinballProductionUpgradeDisplayController : MonoBehaviour
{
    [SerializeField] private EPinballProductionUpgrade upgrade;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private TextMeshProUGUI costText;

    private PinballManager _pinballManager;
    private BattleManager _battleManager;

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
        _battleManager = App.Get<BattleManager>();
        purchaseButton.onClick.AddListener(Purchase);
        _pinballManager.OnProductionChanged += Refresh;
        _battleManager.OnGoldChanged += OnGoldChanged;
        Refresh();
    }

    private void Purchase()
    {
        _pinballManager.TryPurchaseProductionUpgrade(upgrade);
    }

    private void OnGoldChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_pinballManager == null) return;

        int level = _pinballManager.GetProductionLevel(upgrade);
        int maxLevel = _pinballManager.GetProductionMaxLevel(upgrade);
        bool isMax = level >= maxLevel;
        levelText.text = isMax ? $"Lv. {level} MAX" : $"Lv. {level}";
        effectText.text = isMax
            ? FormatEffect(_pinballManager.GetProductionEffect(upgrade))
            : $"{FormatEffect(_pinballManager.GetProductionEffect(upgrade))} > " +
              FormatEffect(_pinballManager.GetNextProductionEffect(upgrade));
        costText.text = isMax
            ? "MAX"
            : $"{_pinballManager.GetProductionCost(upgrade)}G";
        purchaseButton.interactable =
            _pinballManager.CanPurchaseProductionUpgrade(upgrade);
    }

    private string FormatEffect(float value)
    {
        return upgrade switch
        {
            EPinballProductionUpgrade.BumperIncome => $"{Mathf.RoundToInt(value)}G / HIT",
            EPinballProductionUpgrade.AddBall => $"{Mathf.RoundToInt(value)} BALL",
            EPinballProductionUpgrade.SupplySpeed => $"{value:0.00}s RESPAWN",
            _ => value.ToString("0.##")
        };
    }

    private void OnDestroy()
    {
        if (purchaseButton != null) purchaseButton.onClick.RemoveListener(Purchase);
        if (_pinballManager != null) _pinballManager.OnProductionChanged -= Refresh;
        if (_battleManager != null) _battleManager.OnGoldChanged -= OnGoldChanged;
    }
}
