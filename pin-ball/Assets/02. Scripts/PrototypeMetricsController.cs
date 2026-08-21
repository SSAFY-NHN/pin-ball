using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PrototypeMetricsController : MonoBehaviour
{
    private const int RecentStageCapacity = 10;

    public float RunElapsed => Mathf.Max(0f, Time.time - runStartedAt);
    public float CurrentStageElapsed => currentStageStartedAt < 0f
        ? 0f
        : Mathf.Max(0f, Time.time - currentStageStartedAt);
    public int PermanentBallActivations { get; private set; }
    public int ReleasedPermanentBalls { get; private set; }
    public int GoldenBallActivations { get; private set; }
    public int TotalPermanentBallBumperHits { get; private set; }
    public int NormalBumperGold { get; private set; }
    public int JackpotGold { get; private set; }
    public int JackpotCount { get; private set; }
    public int LastJackpotReward { get; private set; }
    public int TotalJackpotReward { get; private set; }
    public float LastJackpotAt { get; private set; } = -1f;
    public float AverageJackpotInterval => jackpotIntervalCount > 0
        ? totalJackpotInterval / jackpotIntervalCount
        : -1f;
    public int CurrentStage { get; private set; } = 1;
    public bool IsBossStage { get; private set; }
    public float LastStageDuration { get; private set; }
    public int CurrentStageRetryCount { get; private set; }
    public int TotalRetryCount { get; private set; }
    public int CurrentBossRetryCount => IsBossStage ? CurrentStageRetryCount : 0;
    public int LastDefenseLineDamage { get; private set; }
    public int TotalDefenseLineDamage { get; private set; }
    public float FirstStageTenReachedAt { get; private set; } = -1f;
    public float FirstBossDefeatedAt { get; private set; } = -1f;
    public string RecentPurchase { get; private set; } = "--";
    public int ActiveCloneCount => pinballManager?.ActiveCloneCount ?? 0;
    public float BallsPerMinute => PerMinute(PermanentBallActivations);
    public float NormalGoldPerMinute => PerMinute(NormalBumperGold);
    public float JackpotGoldPerMinute => PerMinute(JackpotGold);
    public float AverageHitsPerBall => ReleasedPermanentBalls > 0
        ? (float)TotalPermanentBallBumperHits / ReleasedPermanentBalls
        : 0f;
    public float AverageJackpotReward => JackpotCount > 0
        ? (float)TotalJackpotReward / JackpotCount
        : 0f;

    private readonly Queue<PrototypeStageResult> recentStages = new();
    private readonly float[] firstProductionPurchaseAt = new float[3];
    private readonly float[] firstBattlePurchaseAt = new float[3];
    private BattleManager battleManager;
    private PinballManager pinballManager;
    private float runStartedAt;
    private float currentStageStartedAt = -1f;
    private float previousJackpotAt = -1f;
    private float totalJackpotInterval;
    private int jackpotIntervalCount;
    private bool subscribed;

    private void Start()
    {
        ResetForNewRun();
        Subscribe();
        CaptureExistingState();
    }

    private void ResetForNewRun()
    {
        runStartedAt = Time.time;
        currentStageStartedAt = -1f;
        Array.Fill(firstProductionPurchaseAt, -1f);
        Array.Fill(firstBattlePurchaseAt, -1f);
        recentStages.Clear();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        App.TryGet(out battleManager);
        App.TryGet(out pinballManager);

        if (battleManager != null)
        {
            battleManager.OnStageStarted += OnStageStarted;
            battleManager.OnStageResolved += OnStageResolved;
            battleManager.OnBattleUpgradePurchased += OnBattleUpgradePurchased;
            battleManager.OnAllyPurchased += OnAllyPurchased;
            battleManager.OnBossDefeated += OnBossDefeated;
        }

        if (pinballManager != null)
        {
            pinballManager.OnPermanentBallActivated += OnPermanentBallActivated;
            pinballManager.OnPermanentBallReleased += OnPermanentBallReleased;
            pinballManager.OnBumperRewarded += OnBumperRewarded;
            pinballManager.OnProductionUpgradePurchased += OnProductionUpgradePurchased;
        }

        subscribed = true;
    }

    private void CaptureExistingState()
    {
        if (pinballManager != null)
        {
            foreach (Pinball ball in pinballManager.ActiveBalls)
            {
                PermanentBallActivations++;
                if (ball != null && ball.IsGolden) GoldenBallActivations++;
            }
        }

        if (battleManager != null && battleManager.IsInitialized &&
            battleManager.State == EWaveState.Active && currentStageStartedAt < 0f)
        {
            OnStageStarted(new BattleStageStartedData(
                battleManager.CurrentStageNumber,
                battleManager.IsCurrentStageBoss,
                0));
        }
    }

    private void OnPermanentBallActivated(PinballBallActivatedData data)
    {
        PermanentBallActivations++;
        if (data.IsGolden) GoldenBallActivations++;
    }

    private void OnPermanentBallReleased(PinballBallReleasedData data)
    {
        ReleasedPermanentBalls++;
        TotalPermanentBallBumperHits += Mathf.Max(0, data.BumperHitCount);
    }

    private void OnBumperRewarded(PinballBumperRewardData data)
    {
        NormalBumperGold += Mathf.Max(0, data.NormalGold);
        if (data.IsClone || data.JackpotGold <= 0) return;

        float now = RunElapsed;
        JackpotGold += data.JackpotGold;
        JackpotCount++;
        LastJackpotReward = data.JackpotGold;
        TotalJackpotReward += data.JackpotGold;
        LastJackpotAt = now;
        if (previousJackpotAt >= 0f)
        {
            totalJackpotInterval += now - previousJackpotAt;
            jackpotIntervalCount++;
        }
        previousJackpotAt = now;
    }

    private void OnProductionUpgradePurchased(PinballUpgradePurchasedData data)
    {
        int index = (int)data.Upgrade;
        RecordFirstPurchase(firstProductionPurchaseAt, index);
        RecentPurchase = $"생산 {GetProductionUpgradeName(data.Upgrade)} " +
                         $"Lv.{data.Level} | {FormatTime(RunElapsed)} | {data.Cost}G";
    }

    private void OnBattleUpgradePurchased(BattleUpgradePurchasedData data)
    {
        int index = (int)data.Upgrade;
        RecordFirstPurchase(firstBattlePurchaseAt, index);
        RecentPurchase = $"전투 {GetBattleUpgradeName(data.Upgrade)} " +
                         $"Lv.{data.Level} | {FormatTime(RunElapsed)} | {data.Cost}G";
    }

    private void OnAllyPurchased(UnitPurchaseResult result)
    {
        RecentPurchase = $"전투 {GetAllyName(result.UnitId)} " +
                         $"{result.PurchaseCount}회 | {FormatTime(RunElapsed)} | " +
                         $"{result.Cost}G";
    }

    private void OnStageStarted(BattleStageStartedData data)
    {
        if (data.Stage != CurrentStage) CurrentStageRetryCount = 0;
        CurrentStage = Mathf.Max(1, data.Stage);
        IsBossStage = data.IsBoss;
        currentStageStartedAt = Time.time;
        if (CurrentStage >= 10 && FirstStageTenReachedAt < 0f)
        {
            FirstStageTenReachedAt = RunElapsed;
        }
    }

    private void OnStageResolved(BattleStageResolvedData data)
    {
        LastStageDuration = Mathf.Max(0f, data.Duration);
        LastDefenseLineDamage = Mathf.Max(0, data.DefenseLineDamage);
        TotalDefenseLineDamage += LastDefenseLineDamage;
        if (data.Result == EWaveResolutionResult.Failed)
        {
            CurrentStageRetryCount++;
            TotalRetryCount++;
        }

        recentStages.Enqueue(new PrototypeStageResult(
            data.Stage,
            data.Result,
            data.Duration));
        while (recentStages.Count > RecentStageCapacity) recentStages.Dequeue();
    }

    private void OnBossDefeated(int _)
    {
        if (FirstBossDefeatedAt < 0f) FirstBossDefeatedAt = RunElapsed;
    }

    private float PerMinute(int value)
    {
        float elapsedMinutes = RunElapsed / 60f;
        return elapsedMinutes > 0f ? value / elapsedMinutes : 0f;
    }

    private void RecordFirstPurchase(float[] values, int index)
    {
        if (index >= 0 && index < values.Length && values[index] < 0f)
        {
            values[index] = RunElapsed;
        }
    }

    private static string GetProductionUpgradeName(EPinballProductionUpgrade upgrade)
    {
        return upgrade switch
        {
            EPinballProductionUpgrade.BumperIncome => "범퍼 수익",
            EPinballProductionUpgrade.AddBall => "공 추가",
            EPinballProductionUpgrade.SupplySpeed => "공급 속도",
            _ => upgrade.ToString()
        };
    }

    private static string GetBattleUpgradeName(EBattleUpgrade upgrade)
    {
        return upgrade switch
        {
            EBattleUpgrade.AllyAttack => "아군 공격력",
            EBattleUpgrade.DefenseLineHp => "방어선 체력",
            _ => upgrade.ToString()
        };
    }

    private static string GetAllyName(string unitId)
    {
        return unitId switch
        {
            "warrior" => "전사 구매",
            "archer" => "궁수 구매",
            "mage" => "마법사 구매",
            _ => unitId
        };
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--";
        int totalSeconds = Mathf.FloorToInt(seconds);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        if (battleManager != null)
        {
            battleManager.OnStageStarted -= OnStageStarted;
            battleManager.OnStageResolved -= OnStageResolved;
            battleManager.OnBattleUpgradePurchased -= OnBattleUpgradePurchased;
            battleManager.OnAllyPurchased -= OnAllyPurchased;
            battleManager.OnBossDefeated -= OnBossDefeated;
        }

        if (pinballManager != null)
        {
            pinballManager.OnPermanentBallActivated -= OnPermanentBallActivated;
            pinballManager.OnPermanentBallReleased -= OnPermanentBallReleased;
            pinballManager.OnBumperRewarded -= OnBumperRewarded;
            pinballManager.OnProductionUpgradePurchased -= OnProductionUpgradePurchased;
        }
        subscribed = false;
    }

    private readonly struct PrototypeStageResult
    {
        public int Stage { get; }
        public EWaveResolutionResult Result { get; }
        public float Duration { get; }

        public PrototypeStageResult(
            int stage,
            EWaveResolutionResult result,
            float duration)
        {
            Stage = stage;
            Result = result;
            Duration = duration;
        }
    }
}
