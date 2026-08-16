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
