using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleUpgradeDisplayController : MonoBehaviour
{
    [SerializeField] private EBattleUpgrade upgrade;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private TextMeshProUGUI costText;

    private BattleManager battleManager;

    private void Start()
    {
        battleManager = App.Get<BattleManager>();
        purchaseButton.onClick.AddListener(Purchase);
        battleManager.OnInitialized += Refresh;
        battleManager.OnGoldChanged += OnGoldChanged;
        battleManager.OnBattleUpgradeChanged += Refresh;
        Refresh();
    }

    private void Purchase()
    {
        battleManager.TryPurchaseBattleUpgrade(upgrade);
    }

    private void OnGoldChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (battleManager == null) return;

        int level = battleManager.GetBattleUpgradeLevel(upgrade);
        int maxLevel = battleManager.GetBattleUpgradeMaxLevel(upgrade);
        bool isMax = level >= maxLevel;
        nameText.text = GetName();
        levelText.text = isMax ? $"Lv. {level} MAX" : $"Lv. {level}";
        effectText.text = isMax
            ? FormatEffect(battleManager.GetBattleUpgradeEffect(upgrade))
            : $"{FormatEffect(battleManager.GetBattleUpgradeEffect(upgrade))} > " +
              FormatEffect(battleManager.GetNextBattleUpgradeEffect(upgrade));
        costText.text = isMax
            ? "MAX"
            : $"{battleManager.GetBattleUpgradeCost(upgrade)}G";
        purchaseButton.interactable =
            battleManager.CanPurchaseBattleUpgrade(upgrade);
    }

    private string GetName()
    {
        return upgrade switch
        {
            EBattleUpgrade.AllyAttack => "아군 공격력",
            EBattleUpgrade.DefenseLineHp => "방어선 체력",
            _ => string.Empty
        };
    }

    private string FormatEffect(float value)
    {
        return upgrade switch
        {
            EBattleUpgrade.AllyAttack => $"x{value:0.00}",
            EBattleUpgrade.DefenseLineHp => $"+{Mathf.RoundToInt(value)} HP",
            _ => value.ToString("0.##")
        };
    }

    private void OnDestroy()
    {
        if (purchaseButton != null) purchaseButton.onClick.RemoveListener(Purchase);
        if (battleManager == null) return;

        battleManager.OnInitialized -= Refresh;
        battleManager.OnGoldChanged -= OnGoldChanged;
        battleManager.OnBattleUpgradeChanged -= Refresh;
    }
}
