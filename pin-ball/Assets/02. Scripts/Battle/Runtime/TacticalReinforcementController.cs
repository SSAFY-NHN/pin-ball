using System;

using UnityEngine;

public sealed class TacticalReinforcementController
{
    public bool HasTicket { get; private set; }

    private readonly int comboThreshold;
    private bool rewardedCurrentCombo;

    public TacticalReinforcementController(int comboThreshold)
    {
        this.comboThreshold = Mathf.Max(1, comboThreshold);
    }

    public bool ObserveCombo(int combo)
    {
        if (combo <= 0)
        {
            rewardedCurrentCombo = false;
            return false;
        }

        if (rewardedCurrentCombo || combo < comboThreshold) return false;

        rewardedCurrentCombo = true;
        return Grant();
    }

    public bool GrantFromJackpot()
    {
        return Grant();
    }

    public bool Consume()
    {
        if (!HasTicket) return false;

        HasTicket = false;
        return true;
    }

    public bool TryUse(Func<bool> trySpawn)
    {
        if (!HasTicket || trySpawn == null || !trySpawn()) return false;

        return Consume();
    }

    private bool Grant()
    {
        if (HasTicket) return false;

        HasTicket = true;
        return true;
    }
}
