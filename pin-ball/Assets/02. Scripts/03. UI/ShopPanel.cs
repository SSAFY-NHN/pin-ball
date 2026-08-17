using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : UIBase
{
    public override bool IsDefaultPanel => true;
    public override bool IsManagedByStack => false;
    private int DefaultShopItemCount => itemSlots?.Length ?? 0;

    [Header("Slots")]
    [SerializeField] private ShopSlot[] itemSlots;
    
    [Header("Reroll")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField, Min(0)] private int rerollCost;

    private readonly List<Item> _candidateItems = new();
    private ItemManager _itemManager;
    private BattleManager _battleManager;
    private ShopRerollController _rerollController;
    private ShopPurchasePolicyController _purchasePolicy;

    public void SetTutorialPurchaseRestriction(EItem? item)
    {
        _purchasePolicy?.SetTutorialRestriction(item);
        RefreshPurchaseStates();
    }

    public ShopSlot FindSlot(EItem item)
    {
        if (itemSlots == null) return null;
        foreach (var slot in itemSlots)
        {
            if (slot != null && slot.Item != null && slot.Item.Key == item)
            {
                return slot;
            }
        }
        return null;
    }

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _itemManager = App.Get<ItemManager>();
        _battleManager = App.Get<BattleManager>();
        var offerController = new ShopOfferController(_itemManager);
        _rerollController = new ShopRerollController(
            _battleManager,
            offerController,
            rerollCost);
        _purchasePolicy = new ShopPurchasePolicyController(_itemManager);
        _battleManager.OnGoldChanged += OnGoldChanged;
        _battleManager.OnStateChanged += OnBattleStateChanged;
        _battleManager.OnPreparationAvailabilityChanged +=
            OnPreparationAvailabilityChanged;

        rerollButton.onClick.AddListener(OnRerollButtonClicked);
        
        if (rerollCostText != null)
        {
            rerollCostText.text = rerollCost <= 0
                ? "무료"
                : rerollCost.ToString();
        }
        
        ValidateItemSlots();
        RefreshItems(true);
    }

    private void OnRerollButtonClicked()
    {
        if (!_rerollController.TryReroll(
                _candidateItems,
                DefaultShopItemCount))
        {
            RefreshPurchaseStates();
            return;
        }

        DisplayItems();
    }

    private void RefreshItems(bool guaranteePotions)
    {
        _rerollController.RefreshOffers(
            _candidateItems,
            DefaultShopItemCount,
            guaranteePotions);
        DisplayItems();
    }

    private void DisplayItems()
    {
        if (itemSlots == null) return;

        for (var i = 0; i < itemSlots.Length; i++)
        {
            var slot = itemSlots[i];
            if (slot == null) continue;

            if (i < DefaultShopItemCount && i < _candidateItems.Count)
            {
                slot.gameObject.SetActive(true);
                slot.SetItem(_candidateItems[i], OnPurchaseButtonClicked);
            }
            else
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
        }

        RefreshPurchaseStates();
    }

    private void OnPurchaseButtonClicked(Item item)
    {
        _itemManager.TryPurchase(item);
        RefreshPurchaseStates();
    }

    private void OnGoldChanged(int _)
    {
        RefreshPurchaseStates();
    }

    private void OnBattleStateChanged(EWaveState _)
    {
        if (_battleManager.State == EWaveState.Starting)
        {
            RefreshItems(true);
            return;
        }

        RefreshPurchaseStates();
    }

    private void OnPreparationAvailabilityChanged(bool _)
    {
        RefreshPurchaseStates();
    }

    private void RefreshPurchaseStates()
    {
        if (_battleManager == null || _itemManager == null) return;
        if (itemSlots == null) return;

        foreach (var slot in itemSlots)
        {
            if (slot == null) continue;

            var item = slot.Item;
            var isPurchased = item != null &&
                              !_purchasePolicy.CanPurchase(item);
            slot.RefreshState(
                _battleManager.Gold,
                isPurchased,
                _battleManager.CanUsePreparationActions);
        }

        if (rerollButton != null)
        {
            rerollButton.interactable =
                _battleManager.CanUsePreparationActions &&
                (rerollCost <= 0 || _battleManager.Gold >= rerollCost);
        }
    }

    private void ValidateItemSlots()
    {
        if (itemSlots == null || itemSlots.Length == 0)
        {
            Debug.LogWarning(
                "[ShopPanel] Item Slot을 Inspector에 등록해야 합니다.");
        }
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnGoldChanged -= OnGoldChanged;
            _battleManager.OnStateChanged -= OnBattleStateChanged;
            _battleManager.OnPreparationAvailabilityChanged -=
                OnPreparationAvailabilityChanged;
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(OnRerollButtonClicked);
        }
    }

}
