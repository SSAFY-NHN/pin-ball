using System.Collections.Generic;

using UnityEngine;

internal sealed class ItemCatalogController
{
    private const string ItemIconPath = "ItemIcons/";

    private readonly Dictionary<EItem, Item> _items = new();

    private bool _isInitialized;

    public void EnsureInitialized()
    {
        if (_isInitialized) return;

        var titleData = App.Get<TitleData>();
        foreach (var data in titleData.Item)
        {
            var icon = Resources.Load<Sprite>($"{ItemIconPath}{data.Value.id}");
            var item = new Item(data.Value, icon);
            _items.Add(item.Key, item);
        }

        _isInitialized = true;
    }

    public bool TryGetItem(EItem item, out Item result) =>
        _items.TryGetValue(item, out result);

    public void GetItems(List<Item> result)
    {
        result.Clear();
        result.AddRange(_items.Values);
    }

    public void GetActiveItems(
        List<Item> result,
        ItemInventoryController inventory)
    {
        result.Clear();

        foreach (var item in _items.Values)
        {
            if (inventory.HasItem(item.Key))
            {
                result.Add(item);
            }
        }
    }
}
