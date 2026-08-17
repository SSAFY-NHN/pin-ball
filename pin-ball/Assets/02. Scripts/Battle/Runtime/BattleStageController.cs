using UnityEngine;

public sealed class BattleStageController
{
    public int CurrentStage { get; private set; } = 1;
    public EWaveState State { get; private set; } = EWaveState.Starting;
    public float TransitionEndsAt { get; private set; }

    public bool TryStart()
    {
        if (State != EWaveState.Starting) return false;
        State = EWaveState.Active;
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
