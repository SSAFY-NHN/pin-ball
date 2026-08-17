using UnityEngine;

public sealed class PinballComboController
{
    private const float ComboDuration = 2f;

    public int Count { get; private set; }

    private float _expiresAt;

    public int RegisterBumperHit(float currentTime)
    {
        Count++;
        _expiresAt = currentTime + ComboDuration;
        return Count;
    }

    public float GetRewardMultiplier(
        int hitsPerStep,
        float multiplierPerStep,
        float maximumMultiplier)
    {
        int safeHitsPerStep = Mathf.Max(1, hitsPerStep);
        float safeStep = Mathf.Max(0f, multiplierPerStep);
        float safeMaximum = Mathf.Max(1f, maximumMultiplier);
        int completedSteps = Mathf.Max(0, (Count - 1) / safeHitsPerStep);
        return Mathf.Min(1f + completedSteps * safeStep, safeMaximum);
    }

    public bool TryExpire(float currentTime)
    {
        if (Count <= 0 || currentTime < _expiresAt) return false;

        Reset();
        return true;
    }

    public float GetRemainingProgress(float currentTime)
    {
        if (Count <= 0) return 0f;
        return Mathf.Clamp01((_expiresAt - currentTime) / ComboDuration);
    }

    public void Reset()
    {
        Count = 0;
        _expiresAt = 0f;
    }
}
