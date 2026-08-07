using System;
using System.Collections.Generic;

using UnityEngine;

[Serializable]
public struct BattleUnitStats
{
    [Min(1f)] public float MaxHp;
    [Min(0f)] public float AttackDamage;
    [Min(0f)] public float Defense;
    [Min(0.01f)] public float AttackRate;
    [Min(0.1f)] public float AttackRange;
    [Min(0f)] public float MoveSpeed;
    [Min(0f)] public float MaxMana;
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
    public string UnitId = "warrior";
    [Range(1, 10)] public int Level = 1;
    public BattleUnitModifier Modifier;
}

[Serializable]
public class BattleEnemySpawnData
{
    public string EnemyId = "goblin";
    [Min(1)] public int Count = 1;
}

[Serializable]
public class BattleWaveData
{
    public string WaveName = "Wave";
    [Min(0)] public int WaveClearGoldReward = 5; // TODO: JSON 웨이브 보상 데이터로 교체
    public List<BattleEnemySpawnData> Enemies = new();
}
