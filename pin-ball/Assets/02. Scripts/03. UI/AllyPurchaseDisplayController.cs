using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AllyPurchaseDisplayController : MonoBehaviour
{
    [SerializeField] private string unitId;
    [SerializeField] private string unitName;
    [SerializeField] private string role;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI displayText;

    private BattleManager battleManager;
    private UnitManager unitManager;

    private void Start()
    {
        battleManager = App.Get<BattleManager>();
        unitManager = App.Get<UnitManager>();
        purchaseButton.onClick.AddListener(Purchase);
        battleManager.OnInitialized += Refresh;
        battleManager.OnGoldChanged += OnGoldChanged;
        battleManager.OnAllyPurchased += OnAllyPurchased;
        unitManager.OnDeployedAllyCountChanged += OnDeployedAllyCountChanged;
        Refresh();
    }

    private void Purchase()
    {
        battleManager.TryPurchaseAlly(unitId);
    }

    private void OnGoldChanged(int _)
    {
        Refresh();
    }

    private void OnAllyPurchased(UnitPurchaseResult _)
    {
        Refresh();
    }

    private void OnDeployedAllyCountChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (battleManager == null) return;

        displayText.text = FormatDisplay(
            unitName,
            role,
            battleManager.GetAllyPurchaseCost(unitId),
            battleManager.GetAllyPurchaseCount(unitId));
        purchaseButton.interactable = battleManager.CanPurchaseAlly(unitId);
    }

    public static string FormatDisplay(
        string unitName,
        string role,
        int cost,
        int purchaseCount)
    {
        return $"{unitName}\n{role}\n{purchaseCount}회 · {cost}G";
    }

    private void OnDestroy()
    {
        if (purchaseButton != null) purchaseButton.onClick.RemoveListener(Purchase);
        if (battleManager != null)
        {
            battleManager.OnInitialized -= Refresh;
            battleManager.OnGoldChanged -= OnGoldChanged;
            battleManager.OnAllyPurchased -= OnAllyPurchased;
        }

        if (unitManager != null)
        {
            unitManager.OnDeployedAllyCountChanged -= OnDeployedAllyCountChanged;
        }
    }
}
