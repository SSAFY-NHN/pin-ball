using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AllyPurchasePanelController : MonoBehaviour
{
    private const string WarriorId = "warrior";
    private const string ArcherId = "archer";
    private const string MageId = "mage";

    [SerializeField] private Button warriorPurchaseButton;
    [SerializeField] private TextMeshProUGUI warriorDisplayText;
    [SerializeField] private Button archerPurchaseButton;
    [SerializeField] private TextMeshProUGUI archerDisplayText;
    [SerializeField] private Button magePurchaseButton;
    [SerializeField] private TextMeshProUGUI mageDisplayText;
    [SerializeField] private TextMeshProUGUI reinforcementNotice;

    private BattleManager battleManager;
    private UnitManager unitManager;

    private void Start()
    {
        battleManager = App.Get<BattleManager>();
        unitManager = App.Get<UnitManager>();

        warriorPurchaseButton.onClick.AddListener(PurchaseWarrior);
        archerPurchaseButton.onClick.AddListener(PurchaseArcher);
        magePurchaseButton.onClick.AddListener(PurchaseMage);

        battleManager.OnInitialized += Refresh;
        battleManager.OnGoldChanged += OnGoldChanged;
        battleManager.OnTacticalReinforcementChanged +=
            OnTacticalReinforcementChanged;
        battleManager.OnStateChanged += OnStateChanged;
        unitManager.OnDeployedAllyCountChanged += OnDeployedAllyCountChanged;
        Refresh();
    }

    private void PurchaseWarrior()
    {
        battleManager.TryPurchaseAlly(WarriorId);
    }

    private void PurchaseArcher()
    {
        battleManager.TryPurchaseAlly(ArcherId);
    }

    private void PurchaseMage()
    {
        battleManager.TryPurchaseAlly(MageId);
    }

    private void OnGoldChanged(int _)
    {
        Refresh();
    }

    private void OnTacticalReinforcementChanged(bool _)
    {
        Refresh();
    }

    private void OnStateChanged(EWaveState _)
    {
        Refresh();
    }

    private void OnDeployedAllyCountChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (battleManager == null || unitManager == null) return;

        bool isFree = battleManager.HasTacticalReinforcement;
        RefreshCard(
            WarriorId,
            "전사",
            "근접 탱커",
            warriorPurchaseButton,
            warriorDisplayText,
            isFree);
        RefreshCard(
            ArcherId,
            "궁수",
            "원거리 지속 공격",
            archerPurchaseButton,
            archerDisplayText,
            isFree);
        RefreshCard(
            MageId,
            "마법사",
            "원거리 범위 공격",
            magePurchaseButton,
            mageDisplayText,
            isFree);

        reinforcementNotice.text = FormatReinforcementNotice(isFree);
        reinforcementNotice.gameObject.SetActive(isFree);
    }

    private void RefreshCard(
        string unitId,
        string unitName,
        string role,
        Button purchaseButton,
        TextMeshProUGUI displayText,
        bool isFree)
    {
        displayText.text = FormatCard(
            unitName,
            role,
            battleManager.GetAllyPurchaseCost(unitId),
            unitManager.GetOwnedAllyCount(unitId),
            isFree);
        purchaseButton.interactable = battleManager.CanPurchaseAlly(unitId);
    }

    public static string FormatCard(
        string unitName,
        string role,
        int cost,
        int ownedCount,
        bool isFree)
    {
        string purchaseState = isFree ? "무료" : $"{cost}G";
        return $"{unitName}\n{role}\n보유 {ownedCount} · {purchaseState}";
    }

    public static string FormatReinforcementNotice(bool hasTicket)
    {
        return hasTicket ? "다음 유닛 무료" : string.Empty;
    }

    private void OnDestroy()
    {
        if (warriorPurchaseButton != null)
        {
            warriorPurchaseButton.onClick.RemoveListener(PurchaseWarrior);
        }

        if (archerPurchaseButton != null)
        {
            archerPurchaseButton.onClick.RemoveListener(PurchaseArcher);
        }

        if (magePurchaseButton != null)
        {
            magePurchaseButton.onClick.RemoveListener(PurchaseMage);
        }

        if (battleManager != null)
        {
            battleManager.OnInitialized -= Refresh;
            battleManager.OnGoldChanged -= OnGoldChanged;
            battleManager.OnTacticalReinforcementChanged -=
                OnTacticalReinforcementChanged;
            battleManager.OnStateChanged -= OnStateChanged;
        }

        if (unitManager != null)
        {
            unitManager.OnDeployedAllyCountChanged -=
                OnDeployedAllyCountChanged;
        }
    }
}
