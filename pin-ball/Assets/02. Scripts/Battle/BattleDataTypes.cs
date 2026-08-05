using System;
using System.Collections.Generic;

using UnityEngine;

[Serializable]
public struct BattleActorStats
{
    [Min(1f)] public float MaxHp;
    [Min(0f)] public float AttackDamage;
    [Min(0.01f)] public float AttackRate;
    [Min(0.1f)] public float AttackRange;
    [Min(0f)] public float MoveSpeed;
}

[Serializable]
public struct BattleUnitModifier
{
    [Min(0)] public int MergeTier;
    [Min(0f)] public float MergeAttackBonusPerTier;
    [Min(0f)] public float MergeHpBonusPerTier;
    public float EquipmentAttackBonus;
    public float EquipmentHpBonus;
}

[Serializable]
public class BattleUnitSpawnData
{
    public string UnitId = "Unit";
    public GameObject Prefab;
    public Vector2 SpawnPosition = new(-4f, 0f);
    public BattleActorStats BaseStats;
    public BattleUnitModifier Modifier;
}

[Serializable]
public class BattleEnemySpawnData
{
    public string EnemyId = "Enemy";
    public GameObject Prefab;
    public Vector2 SpawnPosition = new(4f, 0f);
    public BattleActorStats Stats;
    [Min(1)] public int DefenseDamage = 1;
}

[Serializable]
public class BattleWaveData
{
    public string WaveName = "Wave";
    [Min(0)] public int WaveClearGoldReward = 5; // TODO: JSON 웨이브 보상 데이터로 교체
    public List<BattleEnemySpawnData> Enemies = new();
}
