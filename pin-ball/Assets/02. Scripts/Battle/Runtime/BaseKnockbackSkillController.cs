using System;

public enum EBaseKnockbackSkillState
{
    Locked,
    Ready,
    Used
}

public sealed class BaseKnockbackSkillController
{
    public const float UnlockSeconds = 30f;

    public EBaseKnockbackSkillState State { get; private set; } =
        EBaseKnockbackSkillState.Locked;
    public float ElapsedTime { get; private set; }
    public float RemainingTime => Math.Max(0f, UnlockSeconds - ElapsedTime);
    public bool CanUse => State == EBaseKnockbackSkillState.Ready;

    public void StartWave()
    {
        ElapsedTime = 0f;
        State = EBaseKnockbackSkillState.Locked;
    }

    public bool Advance(float deltaTime, bool isActive)
    {
        if (!isActive || State != EBaseKnockbackSkillState.Locked ||
            deltaTime <= 0f) return false;

        EBaseKnockbackSkillState previousState = State;
        int previousSecond = GetDisplayedRemainingSecond();
        ElapsedTime = Math.Min(UnlockSeconds, ElapsedTime + deltaTime);
        if (ElapsedTime >= UnlockSeconds)
        {
            State = EBaseKnockbackSkillState.Ready;
        }

        return previousState != State ||
               previousSecond != GetDisplayedRemainingSecond();
    }

    public bool TryConfirmUse(bool appliedToAnyEnemy)
    {
        if (!appliedToAnyEnemy || State != EBaseKnockbackSkillState.Ready)
        {
            return false;
        }

        State = EBaseKnockbackSkillState.Used;
        return true;
    }

    private int GetDisplayedRemainingSecond()
    {
        return (int)Math.Ceiling(RemainingTime);
    }
}
