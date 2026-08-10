using System;
using System.Collections.Generic;

using UnityEngine;

public sealed class UnitEffectScheduler
{
    private struct DelayedSlow
    {
        public float MoveSpeedMultiplier;
        public float AttackRateMultiplier;
        public float Duration;
        public float ApplyAt;
    }

    private readonly List<DelayedSlow> _delayedSlows = new();
    private int _remainingDamageTicks;
    private float _damagePerTick;
    private float _armorIgnoreRatio;
    private float _damageTickInterval;
    private float _nextDamageTickAt;

    public void ScheduleDamageOverTime(
        float totalDamage,
        float duration,
        float armorIgnoreRatio,
        float now)
    {
        if (totalDamage <= 0f || duration <= 0f) return;

        _remainingDamageTicks = Mathf.Max(1, Mathf.CeilToInt(duration));
        _damagePerTick = totalDamage / _remainingDamageTicks;
        _armorIgnoreRatio = armorIgnoreRatio;
        _damageTickInterval = duration / _remainingDamageTicks;
        _nextDamageTickAt = now + _damageTickInterval;
    }

    public void ScheduleSlow(
        float moveSpeedMultiplier,
        float attackRateMultiplier,
        float duration,
        float delay,
        float now)
    {
        _delayedSlows.Add(new DelayedSlow
        {
            MoveSpeedMultiplier = moveSpeedMultiplier,
            AttackRateMultiplier = attackRateMultiplier,
            Duration = duration,
            ApplyAt = now + Mathf.Max(0f, delay)
        });
    }

    public void Tick(
        float now,
        Action<float, float> applyDamage,
        Action<float, float, float> applySlow)
    {
        if (_remainingDamageTicks > 0 && now >= _nextDamageTickAt)
        {
            applyDamage?.Invoke(_damagePerTick, _armorIgnoreRatio);
            _remainingDamageTicks--;
            _nextDamageTickAt = now + _damageTickInterval;
        }

        for (int i = _delayedSlows.Count - 1; i >= 0; i--)
        {
            DelayedSlow slow = _delayedSlows[i];
            if (now < slow.ApplyAt) continue;

            applySlow?.Invoke(
                slow.MoveSpeedMultiplier,
                slow.AttackRateMultiplier,
                slow.Duration);
            _delayedSlows.RemoveAt(i);
        }
    }

    public void Reset()
    {
        _remainingDamageTicks = 0;
        _damagePerTick = 0f;
        _armorIgnoreRatio = 0f;
        _damageTickInterval = 0f;
        _nextDamageTickAt = 0f;
        _delayedSlows.Clear();
    }
}
