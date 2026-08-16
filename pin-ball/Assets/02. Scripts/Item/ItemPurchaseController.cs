using System;

internal sealed class ItemPurchaseController
{
    private readonly ItemInventoryController _inventory;

    public ItemPurchaseController(ItemInventoryController inventory)
    {
        _inventory = inventory;
    }

    public bool TryPurchase(
        Item item,
        Action<EItem> acquire,
        Action<Item> notifyPurchased)
    {
        if (item == null || !_inventory.CanPurchase(item.Key))
        {
            return false;
        }

        var battleManager = App.Get<BattleManager>();
        if (!battleManager.TrySpendPreparationGold(item.Cost))
        {
            return false;
        }

        acquire(item.Key);
        notifyPurchased(item);
        SoundManager.PlaySFXIfAvailable(SoundName.BuyItem);
        return true;
    }
}
