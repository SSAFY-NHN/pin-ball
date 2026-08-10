using UnityEngine;

public readonly struct UnitModifierSnapshot
{
    public float AttackMultiplier { get; }
    public float AttackRateMultiplier { get; }
    public float HpMultiplier { get; }

    public UnitModifierSnapshot(float attack, float attackRate, float hp)
    {
        AttackMultiplier = attack;
        AttackRateMultiplier = attackRate;
        HpMultiplier = hp;
    }
}

public sealed class BattleUnitModifiers
{
    private float _attackMultiplier = 1f;
    private float _attackRateMultiplier = 1f;
    private float _hpMultiplier = 1f;
    private float _diversityBonusPerType;
    private float _diversityMaxBonus;
    private float _duplicationChance;
    private int _duplicationTier;
    private int _duplicationCount;

    public void Apply(EItem key, float value1, float value2, float value3)
    {
        switch (key)
        {
            case EItem.AttackManual:
                _attackMultiplier = 1f + value1;
                break;
            case EItem.BattleClock:
                _attackRateMultiplier = 1f + value1;
                break;
            case EItem.FieldArmor:
                _hpMultiplier = 1f + value1;
                break;
            case EItem.DuplicationSeal:
                _duplicationChance = value1;
                _duplicationTier = Mathf.RoundToInt(value2);
                _duplicationCount = Mathf.RoundToInt(value3);
                break;
            case EItem.DiversityEmblem:
                _diversityBonusPerType = value1;
                _diversityMaxBonus = value2;
                break;
        }
    }

    public UnitModifierSnapshot GetRosterSnapshot(int distinctUnitTypeCount)
    {
        float diversityBonus = Mathf.Min(
            _diversityMaxBonus,
            Mathf.Max(0, distinctUnitTypeCount) * _diversityBonusPerType);
        return new UnitModifierSnapshot(
            _attackMultiplier + diversityBonus,
            _attackRateMultiplier,
            _hpMultiplier + diversityBonus);
    }

    public bool ShouldDuplicate(
        BattleUnitSpawnData data,
        float randomRoll,
        out int count)
    {
        count = 0;
        if (_duplicationChance <= 0f || data == null) return false;
        if (data.Modifier.MergeTier != _duplicationTier) return false;
        if (randomRoll > _duplicationChance) return false;

        count = _duplicationCount;
        return true;
    }
}
