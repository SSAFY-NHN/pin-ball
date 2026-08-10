using UnityEngine;

public sealed class UnitHealth
{
    private float _shieldAmount;
    private float _shieldUntil;

    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; }
    public float LastDamagedTime { get; private set; }
    public float HpRatio => MaxHp > 0f
        ? Mathf.Clamp01(CurrentHp / MaxHp)
        : 0f;
    public bool IsDead => CurrentHp <= 0f;

    public UnitHealth(float maximumHp = 0f)
    {
        Reset(maximumHp);
    }

    public void Reset(float maximumHp)
    {
        MaxHp = Mathf.Max(0f, maximumHp);
        CurrentHp = MaxHp;
        LastDamagedTime = 0f;
        _shieldAmount = 0f;
        _shieldUntil = 0f;
    }

    public void MarkDead()
    {
        CurrentHp = 0f;
    }

    public UnitDamageResult TakeDamage(
        float incomingDamage,
        float defense,
        float armorIgnoreRatio,
        float damageReduction,
        float now)
    {
        if (IsDead || incomingDamage <= 0f)
        {
            return new UnitDamageResult(0f, 0f, IsDead);
        }

        Refresh(now);
        float effectiveDefense = defense * (1f - Mathf.Clamp01(armorIgnoreRatio));
        float finalDamage = Mathf.Floor(
            incomingDamage * 100f / (100f + effectiveDefense));
        finalDamage *= 1f - Mathf.Clamp01(damageReduction);

        float absorbedDamage = 0f;
        if (_shieldAmount > 0f)
        {
            absorbedDamage = Mathf.Min(_shieldAmount, finalDamage);
            _shieldAmount -= absorbedDamage;
            finalDamage -= absorbedDamage;
        }

        if (finalDamage <= 0f)
        {
            return new UnitDamageResult(0f, absorbedDamage, false);
        }

        float appliedDamage = Mathf.Min(CurrentHp, finalDamage);
        CurrentHp = Mathf.Max(0f, CurrentHp - finalDamage);
        LastDamagedTime = now;
        return new UnitDamageResult(appliedDamage, absorbedDamage, IsDead);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
    }

    public void ApplyShield(float amount, float duration, float now)
    {
        if (IsDead || amount <= 0f || duration <= 0f) return;
        Refresh(now);
        _shieldAmount = Mathf.Max(_shieldAmount, amount);
        _shieldUntil = Mathf.Max(_shieldUntil, now + duration);
    }

    public void Refresh(float now)
    {
        if (now < _shieldUntil) return;
        _shieldAmount = 0f;
    }

    public void ScaleMaximumHp(float multiplier)
    {
        float ratio = HpRatio;
        MaxHp = Mathf.Max(0f, MaxHp * Mathf.Max(0f, multiplier));
        CurrentHp = MaxHp * ratio;
    }
}
