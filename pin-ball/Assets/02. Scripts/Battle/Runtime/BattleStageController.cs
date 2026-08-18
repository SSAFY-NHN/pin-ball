using UnityEngine;

public sealed class BattleStageController
{
    private readonly int bossStageInterval;

    public int CurrentStage { get; private set; } = 1;
    public EWaveState State { get; private set; } = EWaveState.Starting;
    public float TransitionEndsAt { get; private set; }
    public bool IsNextStageScheduled { get; private set; }
    public bool IsCurrentStageBoss => IsBossStage(CurrentStage);

    public BattleStageController(int bossStageInterval = 10)
    {
        this.bossStageInterval = Mathf.Max(1, bossStageInterval);
    }

    public bool IsBossStage(int stage)
    {
        return stage > 0 && stage % bossStageInterval == 0;
    }

    public bool TryStart()
    {
        if (State != EWaveState.Starting) return false;
        State = EWaveState.Active;
        return true;
    }

    public bool TryAbortStart()
    {
        if (State != EWaveState.Active) return false;
        State = EWaveState.Starting;
        return true;
    }

    public bool TryScheduleNextStage(
        float now,
        float duration)
    {
        if (State != EWaveState.Active || IsNextStageScheduled) return false;

        CurrentStage++;
        IsNextStageScheduled = true;
        TransitionEndsAt = now + Mathf.Max(0f, duration);
        return true;
    }

    public bool TryCompleteNextStageSchedule(float now)
    {
        if (!IsNextStageScheduled || now < TransitionEndsAt) return false;

        IsNextStageScheduled = false;
        TransitionEndsAt = 0f;
        return true;
    }

    public bool TryBeginRecovery(float now, float duration)
    {
        if (State != EWaveState.Active || IsNextStageScheduled) return false;

        State = EWaveState.Recovering;
        TransitionEndsAt = now + Mathf.Max(0f, duration);
        return true;
    }

    public bool TryCompleteRecovery(float now)
    {
        if (State != EWaveState.Recovering || now < TransitionEndsAt)
        {
            return false;
        }

        State = EWaveState.Active;
        TransitionEndsAt = 0f;
        return true;
    }
}
