using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : UIBase
{
    public override bool IsDefaultPanel => true;
    protected override GameObject Panel => panel;
    private const int DefaultShopItemCount = 3;

    [Header("Item")]
    [SerializeField] private ShopItemSlot[] itemSlots;

    [Header("Reroll")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField, Min(0)] private int rerollCost;

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    private readonly List<Item> _candidateItems = new();

    private ItemManager _itemManager;
    private BattleManager _battleManager;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _itemManager = App.Get<ItemManager>();
        _battleManager = App.Get<BattleManager>();
        _battleManager.OnGoldChanged += OnGoldChanged;

        rerollButton.onClick.AddListener(OnRerollButtonClicked);

        openButton.onClick.AddListener(Show);
        closeButton.onClick.AddListener(Hide);
        
        if (rerollCostText != null)
        {
            rerollCostText.text = rerollCost <= 0
                ? "무료"
                : rerollCost.ToString();
        }

        ValidateItemSlots();
        RerollItems();

        Hide();
    }

    public override void Show()
    {
        base.Show();
        
        RefreshPurchaseStates();
    }

    private void OnRerollButtonClicked()
    {
        if (rerollCost > 0 && !_battleManager.TrySpendGold(rerollCost))
        {
            RefreshPurchaseStates();
            return;
        }

        RerollItems();
    }

    private void RerollItems()
    {
        BuildCandidateItems();
        ShuffleCandidates();

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

    private void BuildCandidateItems()
    {
        _itemManager.GetItems(_candidateItems);
        _candidateItems.RemoveAll(item =>
            item == null ||
            _itemManager.HasItem(item.Key));
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
        if (item == null || _itemManager.HasItem(item.Key))
        {
            RefreshPurchaseStates();
            return;
        }

        if (!_battleManager.TrySpendGold(item.Cost))
        {
            RefreshPurchaseStates();
            return;
        }

        _itemManager.Raise(item.Key);
        RefreshPurchaseStates();
    }

    private void OnGoldChanged(int _)
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
            var isPurchased = item != null && _itemManager.HasItem(item.Key);
            slot.RefreshState(_battleManager.Gold, isPurchased);
        }

        if (rerollButton != null)
        {
            rerollButton.interactable =
                rerollCost <= 0 || _battleManager.Gold >= rerollCost;
        }
    }

    private void ValidateItemSlots()
    {
        if (itemSlots == null || itemSlots.Length != DefaultShopItemCount)
        {
            Debug.LogWarning(
                $"[ShopPanel] Item Slot은 {DefaultShopItemCount}개를 등록해야 합니다.");
        }
    }

}
