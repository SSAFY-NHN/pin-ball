using UnityEngine;

public sealed class UnitStatusEffects
{
    private float _attackRateUntil;
    private float _attackDamageUntil;
    private float _defenseUntil;
    private float _moveSpeedUntil;
    private float _damageReductionUntil;
    private float _stunnedUntil;
    private float _knockbackImmuneUntil;

    public float AttackRateMultiplier { get; private set; }
    public float AttackDamageMultiplier { get; private set; }
    public float DefenseMultiplier { get; private set; }
    public float MoveSpeedMultiplier { get; private set; }
    public float DamageReduction { get; private set; }

    public UnitStatusEffects()
    {
        Reset();
    }

    public bool IsStunned(float now) => now < _stunnedUntil;
    public bool IsKnockbackImmune(float now) => now < _knockbackImmuneUntil;

    public void ApplyAttackRateMultiplier(float multiplier, float duration, float now)
    {
        AttackRateMultiplier = Mathf.Max(0.01f, multiplier);
        _attackRateUntil = Mathf.Max(_attackRateUntil, now + duration);
    }

    public void ApplyAttackDamageMultiplier(float multiplier, float duration, float now)
    {
        AttackDamageMultiplier = Mathf.Max(0f, multiplier);
        _attackDamageUntil = Mathf.Max(_attackDamageUntil, now + duration);
    }

    public void ApplyDefenseMultiplier(float multiplier, float duration, float now)
    {
        DefenseMultiplier = Mathf.Max(0f, multiplier);
        _defenseUntil = Mathf.Max(_defenseUntil, now + duration);
    }

    public void ApplyMoveSpeedMultiplier(float multiplier, float duration, float now)
    {
        MoveSpeedMultiplier = Mathf.Max(0f, multiplier);
        _moveSpeedUntil = Mathf.Max(_moveSpeedUntil, now + duration);
    }

    public void ApplyDamageReduction(float ratio, float duration, float now)
    {
        Refresh(now);
        DamageReduction = Mathf.Max(DamageReduction, Mathf.Clamp01(ratio));
        _damageReductionUntil = Mathf.Max(_damageReductionUntil, now + duration);
    }

    public void ApplyStun(float duration, float now)
    {
        _stunnedUntil = Mathf.Max(_stunnedUntil, now + duration);
    }

    public void ApplyKnockbackImmunity(float duration, float now)
    {
        _knockbackImmuneUntil = Mathf.Max(_knockbackImmuneUntil, now + duration);
    }

    public void Refresh(float now)
    {
        if (now >= _attackRateUntil) AttackRateMultiplier = 1f;
        if (now >= _attackDamageUntil) AttackDamageMultiplier = 1f;
        if (now >= _defenseUntil) DefenseMultiplier = 1f;
        if (now >= _moveSpeedUntil) MoveSpeedMultiplier = 1f;
        if (now >= _damageReductionUntil) DamageReduction = 0f;
    }

    public void Reset()
    {
        AttackRateMultiplier = 1f;
        AttackDamageMultiplier = 1f;
        DefenseMultiplier = 1f;
        MoveSpeedMultiplier = 1f;
        DamageReduction = 0f;
        _attackRateUntil = 0f;
        _attackDamageUntil = 0f;
        _defenseUntil = 0f;
        _moveSpeedUntil = 0f;
        _damageReductionUntil = 0f;
        _stunnedUntil = 0f;
        _knockbackImmuneUntil = 0f;
    }
}
