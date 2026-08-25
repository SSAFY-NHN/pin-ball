using System;
using System.Collections.Generic;

using UnityEngine;

public enum EBattleAssaultPhase
{
    Initial,
    Basic,
    Empowered,
    Final
}

public sealed class BattleAssaultController
{
    public const int MaxAliveEnemies = 8;

    public float ElapsedTime { get; private set; }
    public EBattleAssaultPhase Phase { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<EBattleAssaultPhase> PhaseChanged;

    private readonly List<InitialSpawnState> initialSpawns = new();
    private BattleWaveData wave;
    private float initialEndTime;
    private float nextReinforcementTime;

    public void Start(BattleWaveData battleWave)
    {
        wave = battleWave;
        ElapsedTime = 0f;
        Phase = EBattleAssaultPhase.Initial;
        IsRunning = battleWave != null;
        initialSpawns.Clear();
        initialEndTime = 0f;

        if (battleWave?.InitialAssault != null)
        {
            foreach (BattleTimedSpawnData entry in battleWave.InitialAssault)
            {
                if (entry == null) continue;
                initialSpawns.Add(new InitialSpawnState(entry));
                initialEndTime = Mathf.Max(
                    initialEndTime,
                    entry.FirstSpawnTime +
                    entry.SpawnInterval * Mathf.Max(0, entry.Count - 1));
            }
        }

        nextReinforcementTime = initialEndTime +
            GetGroup(EBattleAssaultPhase.Basic).RepeatInterval;
    }

    public void Advance(
        float deltaTime,
        int aliveEnemyCount,
        Func<string, bool> trySpawn)
    {
        if (!IsRunning || trySpawn == null) return;

        ElapsedTime += Mathf.Max(0f, deltaTime);
        int occupiedSlots = Mathf.Max(0, aliveEnemyCount);
        ProcessInitialSpawns(ref occupiedSlots, trySpawn);
        UpdatePhase();
        if (Phase == EBattleAssaultPhase.Initial ||
            ElapsedTime < nextReinforcementTime) return;

        SpawnGroup(GetGroup(Phase), ref occupiedSlots, trySpawn);
        nextReinforcementTime = ElapsedTime + GetGroup(Phase).RepeatInterval;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    private void ProcessInitialSpawns(
        ref int occupiedSlots,
        Func<string, bool> trySpawn)
    {
        foreach (InitialSpawnState state in initialSpawns)
        {
            while (state.RemainingCount > 0 &&
                   ElapsedTime >= state.NextSpawnTime)
            {
                if (occupiedSlots < MaxAliveEnemies && trySpawn(state.EnemyId))
                {
                    occupiedSlots++;
                }
                state.Consume();
            }
        }
    }

    private void UpdatePhase()
    {
        EBattleAssaultPhase nextPhase = ElapsedTime >= 90f
            ? EBattleAssaultPhase.Final
            : ElapsedTime >= 60f
                ? EBattleAssaultPhase.Empowered
                : IsInitialComplete()
                    ? EBattleAssaultPhase.Basic
                    : EBattleAssaultPhase.Initial;
        if (nextPhase == Phase) return;

        Phase = nextPhase;
        nextReinforcementTime = nextPhase switch
        {
            EBattleAssaultPhase.Empowered => 60f,
            EBattleAssaultPhase.Final => 90f,
            _ => initialEndTime + GetGroup(nextPhase).RepeatInterval
        };
        PhaseChanged?.Invoke(Phase);
    }

    private bool IsInitialComplete()
    {
        foreach (InitialSpawnState state in initialSpawns)
        {
            if (state.RemainingCount > 0) return false;
        }
        return true;
    }

    private BattleReinforcementGroupData GetGroup(EBattleAssaultPhase phase)
    {
        BattleReinforcementGroupData group = phase switch
        {
            EBattleAssaultPhase.Empowered => wave?.EmpoweredReinforcement,
            EBattleAssaultPhase.Final => wave?.FinalAssault,
            _ => wave?.BasicReinforcement
        };
        return group ?? new BattleReinforcementGroupData();
    }

    private static void SpawnGroup(
        BattleReinforcementGroupData group,
        ref int occupiedSlots,
        Func<string, bool> trySpawn)
    {
        if (group?.Enemies == null) return;

        foreach (BattleEnemySpawnData entry in group.Enemies)
        {
            if (entry == null) continue;
            for (var count = 0; count < Mathf.Max(0, entry.Count); count++)
            {
                if (occupiedSlots >= MaxAliveEnemies) return;
                if (trySpawn(entry.EnemyId)) occupiedSlots++;
            }
        }
    }

    private sealed class InitialSpawnState
    {
        public string EnemyId { get; }
        public int RemainingCount { get; private set; }
        public float NextSpawnTime { get; private set; }

        private readonly float interval;

        public InitialSpawnState(BattleTimedSpawnData data)
        {
            EnemyId = data.EnemyId;
            RemainingCount = Mathf.Max(0, data.Count);
            NextSpawnTime = Mathf.Max(0f, data.FirstSpawnTime);
            interval = Mathf.Max(0f, data.SpawnInterval);
        }

        public void Consume()
        {
            RemainingCount--;
            NextSpawnTime += interval;
        }
    }
}
