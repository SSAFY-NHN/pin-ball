using System.Collections.Generic;

using UnityEngine;

internal sealed class ShopOfferController
{
    private readonly ItemManager _itemManager;

    public ShopOfferController(ItemManager itemManager)
    {
        _itemManager = itemManager;
    }

    public void BuildOffers(
        List<Item> result,
        int slotCount,
        bool guaranteePotions)
    {
        _itemManager.GetItems(result);
        result.RemoveAll(item =>
            item == null ||
            item.Key != EItem.PersonalHealingPotion &&
            item.Key != EItem.PartyHealingPotion &&
            !_itemManager.CanPurchase(item.Key));

        Shuffle(result, slotCount);

        if (!guaranteePotions) return;

        PinItem(result, EItem.PartyHealingPotion, 0, slotCount);
        PinItem(result, EItem.PersonalHealingPotion, 1, slotCount);
    }

    private static void Shuffle(List<Item> items, int slotCount)
    {
        int drawCount = Mathf.Min(slotCount, items.Count);
        for (var i = 0; i < drawCount; i++)
        {
            int randomIndex = Random.Range(i, items.Count);
            (items[i], items[randomIndex]) =
                (items[randomIndex], items[i]);
        }
    }

    private static void PinItem(
        List<Item> items,
        EItem key,
        int slotIndex,
        int slotCount)
    {
        int itemIndex = items.FindIndex(item => item.Key == key);
        if (itemIndex < 0 ||
            slotIndex >= slotCount ||
            slotIndex >= items.Count)
        {
            return;
        }

        (items[slotIndex], items[itemIndex]) =
            (items[itemIndex], items[slotIndex]);
    }
}
