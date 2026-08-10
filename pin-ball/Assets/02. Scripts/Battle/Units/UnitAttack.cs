using UnityEngine;

public sealed class UnitAttack
{
    public float NextAttackTime { get; private set; }

    public void Reset()
    {
        NextAttackTime = 0f;
    }

    public bool TrySchedule(float now, float effectiveAttackRate)
    {
        if (now < NextAttackTime) return false;

        float rate = Mathf.Max(0.01f, effectiveAttackRate);
        NextAttackTime = now + 1f / rate;
        return true;
    }
}
