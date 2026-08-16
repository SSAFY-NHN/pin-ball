using System.Collections.Generic;

internal sealed class ShopRerollController
{
    private readonly BattleManager _battleManager;
    private readonly ShopOfferController _offerController;
    private readonly int _rerollCost;

    public ShopRerollController(
        BattleManager battleManager,
        ShopOfferController offerController,
        int rerollCost)
    {
        _battleManager = battleManager;
        _offerController = offerController;
        _rerollCost = rerollCost;
    }

    public bool TryReroll(List<Item> result, int slotCount)
    {
        if (!_battleManager.CanUsePreparationActions)
        {
            return false;
        }

        if (_rerollCost > 0 &&
            !_battleManager.TrySpendPreparationGold(_rerollCost))
        {
            return false;
        }

        _offerController.BuildOffers(result, slotCount, false);
        return true;
    }

    public void RefreshOffers(
        List<Item> result,
        int slotCount,
        bool guaranteePotions)
    {
        _offerController.BuildOffers(
            result,
            slotCount,
            guaranteePotions);
    }
}
