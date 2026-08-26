using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AllyPurchasePanelController : MonoBehaviour
{
    [Serializable]
    private sealed class AdvancedPurchaseCard
    {
        public string unitId;
        public string unitName;
        public string role;
        public int unlockLevel;
        public Button purchaseButton;
        public TextMeshProUGUI displayText;
        public Image cooldownMask;
        public TextMeshProUGUI cooldownText;
        [NonSerialized] public int displayedCooldownSecond = int.MinValue;
    }

    private const string WarriorId = "warrior";
    private const string ArcherId = "archer";
    private const string MageId = "mage";
    private const string SpearmanId = "spearman";

    [SerializeField] private Button warriorPurchaseButton;
    [SerializeField] private TextMeshProUGUI warriorDisplayText;
    [SerializeField] private Image warriorPortraitImage;
    [SerializeField] private Image warriorCooldownMask;
    [SerializeField] private TextMeshProUGUI warriorCooldownText;
    [SerializeField] private Button archerPurchaseButton;
    [SerializeField] private TextMeshProUGUI archerDisplayText;
    [SerializeField] private Image archerPortraitImage;
    [SerializeField] private Image archerCooldownMask;
    [SerializeField] private TextMeshProUGUI archerCooldownText;
    [SerializeField] private Button magePurchaseButton;
    [SerializeField] private TextMeshProUGUI mageDisplayText;
    [SerializeField] private Image magePortraitImage;
    [SerializeField] private Image mageCooldownMask;
    [SerializeField] private TextMeshProUGUI mageCooldownText;
    [SerializeField] private Button spearmanPurchaseButton;
    [SerializeField] private TextMeshProUGUI spearmanDisplayText;
    [SerializeField] private Image spearmanPortraitImage;
    [SerializeField] private Image spearmanCooldownMask;
    [SerializeField] private TextMeshProUGUI spearmanCooldownText;
    [SerializeField] private TextMeshProUGUI reinforcementNotice;
    [SerializeField] private AdvancedPurchaseCard[] advancedCards;

    private BattleManager battleManager;
    private UnitManager unitManager;
    private int warriorCooldownSecond = int.MinValue;
    private int archerCooldownSecond = int.MinValue;
    private int mageCooldownSecond = int.MinValue;
    private int spearmanCooldownSecond = int.MinValue;

    private void Start()
    {
        battleManager = App.Get<BattleManager>();
        unitManager = App.Get<UnitManager>();

        warriorPurchaseButton.onClick.AddListener(PurchaseWarrior);
        archerPurchaseButton.onClick.AddListener(PurchaseArcher);
        magePurchaseButton.onClick.AddListener(PurchaseMage);
        spearmanPurchaseButton.onClick.AddListener(PurchaseSpearman);
        if (advancedCards != null)
        {
            foreach (AdvancedPurchaseCard card in advancedCards)
            {
                AdvancedPurchaseCard captured = card;
                card?.purchaseButton?.onClick.AddListener(
                    () => PurchaseAdvanced(captured));
            }
        }

        battleManager.OnInitialized += Refresh;
        battleManager.OnGoldChanged += OnGoldChanged;
        battleManager.OnTacticalReinforcementChanged +=
            OnTacticalReinforcementChanged;
        battleManager.OnStateChanged += OnStateChanged;
        unitManager.OnDeployedAllyCountChanged += OnDeployedAllyCountChanged;
        Refresh();
    }

    private void Update()
    {
        if (battleManager == null || unitManager == null) return;

        RefreshCooldown(
            WarriorId,
            warriorPurchaseButton,
            warriorCooldownMask,
            warriorCooldownText,
            ref warriorCooldownSecond);
        RefreshCooldown(
            ArcherId,
            archerPurchaseButton,
            archerCooldownMask,
            archerCooldownText,
            ref archerCooldownSecond);
        RefreshCooldown(
            MageId,
            magePurchaseButton,
            mageCooldownMask,
            mageCooldownText,
            ref mageCooldownSecond);
        RefreshCooldown(
            SpearmanId,
            spearmanPurchaseButton,
            spearmanCooldownMask,
            spearmanCooldownText,
            ref spearmanCooldownSecond);
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

    private void PurchaseSpearman()
    {
        battleManager.TryPurchaseAlly(SpearmanId);
    }

    private void PurchaseAdvanced(AdvancedPurchaseCard card)
    {
        if (card != null) battleManager.TryPurchaseAlly(card.unitId);
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
            "돌진 저지 · 전열 방어",
            warriorPurchaseButton,
            warriorDisplayText,
            warriorCooldownMask,
            warriorCooldownText,
            isFree,
            ref warriorCooldownSecond);
        RefreshCard(
            ArcherId,
            "궁수",
            "장거리 · 단일 지속 피해",
            archerPurchaseButton,
            archerDisplayText,
            archerCooldownMask,
            archerCooldownText,
            isFree,
            ref archerCooldownSecond);
        RefreshCard(
            MageId,
            "마법사",
            "원거리 · 범위 피해",
            magePurchaseButton,
            mageDisplayText,
            mageCooldownMask,
            mageCooldownText,
            isFree,
            ref mageCooldownSecond);
        RefreshCard(
            SpearmanId,
            "창병",
            "중거리 · 방어 관통",
            spearmanPurchaseButton,
            spearmanDisplayText,
            spearmanCooldownMask,
            spearmanCooldownText,
            isFree,
            ref spearmanCooldownSecond);
        if (advancedCards != null)
        {
            foreach (AdvancedPurchaseCard card in advancedCards)
            {
                RefreshAdvancedCard(card, isFree);
            }
        }

        UiRefreshUtility.SetTextIfChanged(
            reinforcementNotice,
            FormatReinforcementNotice(isFree));
        UiRefreshUtility.SetActiveIfChanged(
            reinforcementNotice.gameObject,
            isFree);
    }

    private void RefreshAdvancedCard(AdvancedPurchaseCard card, bool isFree)
    {
        if (card == null || card.purchaseButton == null) return;
        bool unlocked = battleManager.IsAllyJobUnlocked(card.unitId);
        string text = unlocked
            ? FormatCard(
                card.unitName,
                card.role,
                battleManager.GetAllyPurchaseCost(card.unitId),
                unitManager.GetOwnedAllyCount(card.unitId),
                isFree)
            : $"{card.unitName}\n{card.role}\nLv.{card.unlockLevel} 해금";
        UiRefreshUtility.SetTextIfChanged(card.displayText, text);
        RefreshCooldown(
            card.unitId,
            card.purchaseButton,
            card.cooldownMask,
            card.cooldownText,
            ref card.displayedCooldownSecond);
        if (!unlocked) card.purchaseButton.interactable = false;
    }

    private void RefreshCard(
        string unitId,
        string unitName,
        string role,
        Button purchaseButton,
        TextMeshProUGUI displayText,
        Image cooldownMask,
        TextMeshProUGUI cooldownText,
        bool isFree,
        ref int displayedCooldownSecond)
    {
        UiRefreshUtility.SetTextIfChanged(
            displayText,
            FormatCard(
                unitName,
                role,
                battleManager.GetAllyPurchaseCost(unitId),
                unitManager.GetOwnedAllyCount(unitId),
                isFree));
        RefreshCooldown(
            unitId,
            purchaseButton,
            cooldownMask,
            cooldownText,
            ref displayedCooldownSecond);
    }

    private void RefreshCooldown(
        string unitId,
        Button purchaseButton,
        Image cooldownMask,
        TextMeshProUGUI cooldownText,
        ref int displayedCooldownSecond)
    {
        bool canPurchase = battleManager.CanPurchaseAlly(unitId);
        if (purchaseButton.interactable != canPurchase)
        {
            purchaseButton.interactable = canPurchase;
        }

        float remaining = battleManager.GetAllyRemainingCooldown(unitId);
        int cooldownSecond = remaining > 0f
            ? Mathf.CeilToInt(remaining)
            : 0;
        if (displayedCooldownSecond == cooldownSecond) return;

        displayedCooldownSecond = cooldownSecond;
        UiRefreshUtility.SetActiveIfChanged(
            cooldownMask.gameObject,
            cooldownSecond > 0);
        UiRefreshUtility.SetTextIfChanged(
            cooldownText,
            cooldownSecond > 0 ? cooldownSecond.ToString() : string.Empty);
    }

    public static string FormatCooldown(float remainingSeconds)
    {
        return remainingSeconds > 0f
            ? Mathf.CeilToInt(remainingSeconds).ToString()
            : string.Empty;
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

        if (spearmanPurchaseButton != null)
        {
            spearmanPurchaseButton.onClick.RemoveListener(PurchaseSpearman);
        }

        if (advancedCards != null)
        {
            foreach (AdvancedPurchaseCard card in advancedCards)
            {
                card?.purchaseButton?.onClick.RemoveAllListeners();
            }
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
