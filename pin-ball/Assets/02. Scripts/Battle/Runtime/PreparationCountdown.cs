using System;

public sealed class PreparationCountdown
{
    private readonly float duration;
    private bool expirationReported;

    public float RemainingTime { get; private set; }

    public PreparationCountdown(float duration)
    {
        this.duration = Math.Max(0f, duration);
        Reset();
    }

    public void Reset()
    {
        RemainingTime = duration;
        expirationReported = false;
    }

    public bool Advance(float deltaTime)
    {
        if (expirationReported || deltaTime <= 0f) return false;

        RemainingTime = Math.Max(0f, RemainingTime - deltaTime);
        if (RemainingTime > 0f) return false;

        expirationReported = true;
        return true;
    }
}
