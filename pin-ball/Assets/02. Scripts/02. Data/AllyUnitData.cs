using System;

[Serializable]
public class AllyCommonData
{
    public int maxLevel;
    public int classLevel;
    public float startMana;
    public float basicAttackManaGain;
    public float hitManaGain;
    public float hitManaGainCooldown;
}

[Serializable]
public class AllySkillEffectData
{
    public int effect;
    public int target;
    public float value1;
    public float value2;
    public float value3;
}

[Serializable]
public class AllySkillData
{
    public string id;
    public string name;
    public string description;
    public AllySkillEffectData[] effects;
}

[Serializable]
public class AllyUnitData
{
    public string id;
    public string previousJob;
    public string name;
    public string role;
    public int health;
    public int attack;
    public int defense;
    public float moveSpeed;
    public float attackSpeed;
    public float attackRange;
    public int mana;
    public int healthGrowth;
    public int attackGrowth;
    public int defenseGrowth;
    public float attackSpeedGrowth;
    public AllySkillData skill;

    public BattleUnitStats CreateStats(int level, int classLevel)
    {
        int startLevel = string.IsNullOrEmpty(previousJob) ? 1 : classLevel;
        int growthLevel = Math.Max(0, level - startLevel);

        return new BattleUnitStats
        {
            MaxHp = health + healthGrowth * growthLevel,
            AttackDamage = attack + attackGrowth * growthLevel,
            Defense = defense + defenseGrowth * growthLevel,
            MoveSpeed = moveSpeed,
            AttackRate = attackSpeed + attackSpeedGrowth * growthLevel,
            AttackRange = attackRange,
            MaxMana = mana
        };
    }
}
