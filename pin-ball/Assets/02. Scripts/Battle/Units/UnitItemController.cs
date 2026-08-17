using System.Collections.Generic;

public sealed class UnitItemController
{
    private readonly ItemManager _itemManager;
    private readonly BattleUnitModifiers _unitModifiers = new();
    private readonly HashSet<string> _unitTypes = new();

    public UnitItemController(ItemManager itemManager)
    {
        _itemManager = itemManager;
    }

    public void Apply(
        Item item,
        IReadOnlyList<UnitBase> activeAllies,
        float sharedAttackMultiplier)
    {
        _unitModifiers.Apply(
            item.Key,
            item.Value1,
            item.Value2,
            item.Value3);
        Refresh(activeAllies, sharedAttackMultiplier);
    }

    public void Refresh(
        IReadOnlyList<UnitBase> activeAllies,
        float sharedAttackMultiplier)
    {
        _unitTypes.Clear();
        foreach (var unit in activeAllies)
        {
            if (unit != null) _unitTypes.Add(unit.name);
        }

        UnitModifierSnapshot snapshot =
            _unitModifiers.GetRosterSnapshot(_unitTypes.Count);
        foreach (var unit in activeAllies)
        {
            if (unit is not AllyUnit ally) continue;
            ally.ApplyItemModifiers(
                snapshot.AttackMultiplier,
                snapshot.AttackRateMultiplier,
                snapshot.HpMultiplier);
            ally.ApplySharedAttackMultiplier(sharedAttackMultiplier);
        }
    }

    public void TryUseAutomaticPotion(IReadOnlyList<UnitBase> activeAllies)
    {
        for (var i = 0; i < activeAllies.Count; i++)
        {
            if (activeAllies[i] is not AllyUnit ally ||
                !ally.IsAlive || ally.HpRatio >= 0.5f) continue;

            if (_itemManager.TryConsume(EItem.PartyHealingPotion))
            {
                HealAll(activeAllies, 0.25f);
            }
            else if (_itemManager.TryConsume(EItem.PersonalHealingPotion))
            {
                ally.Heal(ally.MaxHp * 0.5f);
            }

            break;
        }
    }

    private static void HealAll(IReadOnlyList<UnitBase> activeAllies, float ratio)
    {
        foreach (var unit in activeAllies)
        {
            if (unit is AllyUnit ally && ally.IsAlive)
            {
                ally.Heal(ally.MaxHp * ratio);
            }
        }
    }
}
