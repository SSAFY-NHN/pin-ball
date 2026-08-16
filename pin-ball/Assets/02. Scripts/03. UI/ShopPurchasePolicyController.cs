internal sealed class ShopPurchasePolicyController
{
    private readonly ItemManager _itemManager;
    private EItem? _tutorialRestriction;

    public ShopPurchasePolicyController(ItemManager itemManager)
    {
        _itemManager = itemManager;
    }

    public void SetTutorialRestriction(EItem? item)
    {
        _tutorialRestriction = item;
    }

    public bool CanPurchase(Item item)
    {
        if (item == null || !_itemManager.CanPurchase(item.Key))
        {
            return false;
        }

        return !_tutorialRestriction.HasValue ||
               item.Key == _tutorialRestriction.Value;
    }
}
