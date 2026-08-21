using UnityEngine;

public sealed class WaveResolutionState
{
    public bool IsPending { get; private set; }
    public EWaveResolutionResult Result { get; private set; }
    public int WaveNumber { get; private set; }
    public float EndsAt { get; private set; }

    public bool TryBegin(
        EWaveResolutionResult result,
        int waveNumber,
        float now,
        float duration)
    {
        if (IsPending) return false;
        IsPending = true;
        Result = result;
        WaveNumber = Mathf.Max(1, waveNumber);
        EndsAt = now + Mathf.Max(0f, duration);
        return true;
    }

    public bool IsElapsed(float now) => IsPending && now >= EndsAt;

    public void Clear()
    {
        IsPending = false;
        WaveNumber = 0;
        EndsAt = 0f;
    }
}
