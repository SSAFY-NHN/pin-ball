using System.Collections.Generic;

internal sealed class ItemInventoryController
{
    private readonly HashSet<EItem> _activeItems = new();
    private readonly Dictionary<EItem, int> _itemCounts = new();

    public void Acquire(EItem item)
    {
        _itemCounts.TryGetValue(item, out int count);
        _itemCounts[item] = count + 1;
        _activeItems.Add(item);
    }

    public bool HasItem(EItem item) => _activeItems.Contains(item);

    public int GetItemCount(EItem item) =>
        _itemCounts.TryGetValue(item, out int count) ? count : 0;

    public int GetPurchaseLimit(EItem item) => item switch
    {
        EItem.PersonalHealingPotion => 3,
        EItem.PartyHealingPotion => 1,
        _ => 1
    };

    public bool CanPurchase(EItem item) =>
        GetItemCount(item) < GetPurchaseLimit(item);

    public bool TryConsume(EItem item)
    {
        int count = GetItemCount(item);
        if (count <= 0) return false;

        count--;
        if (count == 0)
        {
            _itemCounts.Remove(item);
            _activeItems.Remove(item);
        }
        else
        {
            _itemCounts[item] = count;
        }

        return true;
    }

    public void Clear()
    {
        _activeItems.Clear();
        _itemCounts.Clear();
    }
}
