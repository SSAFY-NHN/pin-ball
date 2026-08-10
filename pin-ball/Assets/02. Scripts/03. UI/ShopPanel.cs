using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : UIBase
{
    public override bool IsDefaultPanel => true;
    public override bool IsManagedByStack => false;
    private int DefaultShopItemCount => _itemSlots.Length;
    
    [Header("Reroll")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField, Min(0)] private int rerollCost;

    private readonly List<Item> _candidateItems = new();
    private ItemManager _itemManager;
    private BattleManager _battleManager;
    private ShopSlot[] _itemSlots;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _itemManager = App.Get<ItemManager>();
        _battleManager = App.Get<BattleManager>();
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
        
        _itemSlots = GetComponentsInChildren<ShopSlot>();

        ValidateItemSlots();
        RerollItems(true);
    }

    private void OnRerollButtonClicked()
    {
        if (!_battleManager.CanUsePreparationActions)
        {
            RefreshPurchaseStates();
            return;
        }

        if (rerollCost > 0 &&
            !_battleManager.TrySpendPreparationGold(rerollCost))
        {
            RefreshPurchaseStates();
            return;
        }

        RerollItems(false);
    }

    private void RerollItems(bool guaranteePotions)
    {
        BuildCandidateItems();
        ShuffleCandidates();

        if (guaranteePotions)
        {
            PinGuaranteedItem(EItem.PartyHealingPotion, 0);
            PinGuaranteedItem(EItem.PersonalHealingPotion, 1);
        }

        if (_itemSlots == null) return;

        for (var i = 0; i < _itemSlots.Length; i++)
        {
            var slot = _itemSlots[i];
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

    private void BuildCandidateItems()
    {
        _itemManager.GetItems(_candidateItems);
        _candidateItems.RemoveAll(item =>
            item == null ||
            item.Key != EItem.PersonalHealingPotion &&
            item.Key != EItem.PartyHealingPotion &&
            !_itemManager.CanPurchase(item.Key));
    }

    private void PinGuaranteedItem(EItem key, int slotIndex)
    {
        int itemIndex = _candidateItems.FindIndex(item => item.Key == key);
        if (itemIndex < 0 || slotIndex >= DefaultShopItemCount) return;
        (_candidateItems[slotIndex], _candidateItems[itemIndex]) =
            (_candidateItems[itemIndex], _candidateItems[slotIndex]);
    }

    private void ShuffleCandidates()
    {
        var drawCount = Mathf.Min(DefaultShopItemCount, _candidateItems.Count);
        for (var i = 0; i < drawCount; i++)
        {
            var randomIndex = Random.Range(i, _candidateItems.Count);
            (_candidateItems[i], _candidateItems[randomIndex]) =
                (_candidateItems[randomIndex], _candidateItems[i]);
        }
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
        if (_battleManager.State == EWaveState.Pending)
        {
            RerollItems(true);
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
        if (_itemSlots == null) return;

        foreach (var slot in _itemSlots)
        {
            if (slot == null) continue;

            var item = slot.Item;
            var isPurchased = item != null && !_itemManager.CanPurchase(item.Key);
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
        if (_itemSlots == null || _itemSlots.Length != DefaultShopItemCount)
        {
            Debug.LogWarning(
                $"[ShopPanel] Item Slot은 {DefaultShopItemCount}개를 등록해야 합니다.");
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
