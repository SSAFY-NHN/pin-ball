using System.Collections.Generic;

using UnityEngine;

public class ItemPanel : UIBase
{
    public override bool IsDefaultPanel => true;
    public override bool IsManagedByStack => false;

    

    private readonly List<Item> _activeItems = new();
    private ItemManager _itemManager;
    private ItemSlot[] _itemSlots;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _itemManager = App.Get<ItemManager>();
        _itemManager.OnItemAcquired += OnItemAcquired;
        
        _itemSlots = GetComponentsInChildren<ItemSlot>();

        RefreshItems();
    }

    private void OnItemAcquired(Item _)
    {
        RefreshItems();
    }

    private void RefreshItems()
    {
        if (_itemManager == null || _itemSlots == null) return;

        _itemManager.GetActiveItems(_activeItems);

        for (var i = 0; i < _itemSlots.Length; i++)
        {
            var slot = _itemSlots[i];
            if (slot == null) continue;

            if (i < _activeItems.Count)
            {
                slot.gameObject.SetActive(true);
                slot.SetItem(_activeItems[i]);
            }
            else
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
        }

        if (_activeItems.Count > _itemSlots.Length)
        {
            Debug.LogWarning(
                $"[ItemPanel] 슬롯이 부족합니다. " +
                $"보유 아이템: {_activeItems.Count}, 슬롯: {_itemSlots.Length}");
        }
    }
}
