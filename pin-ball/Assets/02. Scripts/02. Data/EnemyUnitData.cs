using System;

using UnityEngine;

[Serializable]
public class EnemyCommonData
{
    public int baseWave;
    public int base_wave;
    public float healthGrowthPerWave;
    public float health_growth_per_wave;
    public float attackGrowthPerWave;
    public float attack_growth_per_wave;
    public int defenseGrowthInterval;
    public int defense_growth_interval;
    public int defenseGrowthValue;
    public int defense_growth_value;
    public float moveSpeedGrowthPerWave;
    public float move_speed_growth_per_wave;
    public float attackSpeedGrowthPerWave;
    public float attack_speed_growth_per_wave;

    public int BaseWave => baseWave != 0 ? baseWave : base_wave;
    public float HealthGrowthPerWave =>
        healthGrowthPerWave != 0f ? healthGrowthPerWave : health_growth_per_wave;
    public float AttackGrowthPerWave =>
        attackGrowthPerWave != 0f ? attackGrowthPerWave : attack_growth_per_wave;
    public int DefenseGrowthInterval =>
        defenseGrowthInterval != 0 ? defenseGrowthInterval : defense_growth_interval;
    public int DefenseGrowthValue =>
        defenseGrowthValue != 0 ? defenseGrowthValue : defense_growth_value;
    public float MoveSpeedGrowthPerWave =>
        moveSpeedGrowthPerWave != 0f
            ? moveSpeedGrowthPerWave
            : move_speed_growth_per_wave;
    public float AttackSpeedGrowthPerWave =>
        attackSpeedGrowthPerWave != 0f
            ? attackSpeedGrowthPerWave
            : attack_speed_growth_per_wave;
}

[Serializable]
public class EnemySkillEffectData
{
    public int effect;
    public int target;
    public float value1;
    public float value2;
    public float value3;
}

[Serializable]
public class EnemySkillData
{
    public string id;
    public string key;
    public string name;
    public string description;
    public EnemySkillEffectData[] effects;

    public string SkillId => string.IsNullOrEmpty(key) ? id : key;
}

[Serializable]
public class EnemyUnitData
{
    public string id;
    public string name;
    public string role;
    public int rank;
    public int health;
    public int attack;
    public int defense;
    public float moveSpeed;
    public float move_speed;
    public float attackSpeed;
    public float attack_speed;
    public float attackRange;
    public float attack_range;
    public int breachDamage;
    public int breach_damage;
    public EnemySkillData[] skills;
    public EnemySkillData[] skill;

    public float MoveSpeed => moveSpeed != 0f ? moveSpeed : move_speed;
    public float AttackSpeed => attackSpeed != 0f ? attackSpeed : attack_speed;
    public float AttackRange => attackRange != 0f ? attackRange : attack_range;
    public int BreachDamage => breachDamage != 0 ? breachDamage : breach_damage;
    public EnemySkillData[] Skills => skills ?? skill;

    public BattleUnitStats CreateStats(int wave, EnemyCommonData common)
    {
        int waveDifference = Mathf.Max(0, wave - common.BaseWave);
        int defenseGrowthCount = common.DefenseGrowthInterval > 0
            ? waveDifference / common.DefenseGrowthInterval
            : 0;

        return new BattleUnitStats
        {
            MaxHp = Mathf.Floor(health *
                (1f + waveDifference * common.HealthGrowthPerWave)),
            AttackDamage = Mathf.Floor(attack *
                (1f + waveDifference * common.AttackGrowthPerWave)),
            Defense = defense + defenseGrowthCount * common.DefenseGrowthValue,
            MoveSpeed = MoveSpeed *
                (1f + waveDifference * common.MoveSpeedGrowthPerWave),
            AttackRate = AttackSpeed *
                (1f + waveDifference * common.AttackSpeedGrowthPerWave),
            AttackRange = AttackRange,
            MaxMana = 0f
        };
    }
}

[Serializable]
public class EnemyUnitDataCollection
{
    public EnemyCommonData common;
    public EnemyUnitData[] units;
}
