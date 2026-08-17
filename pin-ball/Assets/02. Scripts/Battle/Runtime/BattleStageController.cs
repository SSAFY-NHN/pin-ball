using UnityEngine;

public sealed class BattleStageController
{
    private readonly int bossStageInterval;

    public int CurrentStage { get; private set; } = 1;
    public EWaveState State { get; private set; } = EWaveState.Starting;
    public float TransitionEndsAt { get; private set; }
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

    public bool TryBeginTransition(
        EWaveResolutionResult result,
        float now,
        float duration)
    {
        if (State != EWaveState.Active) return false;

        State = result == EWaveResolutionResult.Cleared
            ? EWaveState.Advancing
            : EWaveState.Recovering;
        TransitionEndsAt = now + Mathf.Max(0f, duration);
        return true;
    }

    public bool TryCompleteTransition(float now)
    {
        if ((State != EWaveState.Advancing &&
             State != EWaveState.Recovering) ||
            now < TransitionEndsAt)
        {
            return false;
        }

        if (State == EWaveState.Advancing) CurrentStage++;
        State = EWaveState.Active;
        TransitionEndsAt = 0f;
        return true;
    }
}
