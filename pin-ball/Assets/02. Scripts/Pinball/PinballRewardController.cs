using UnityEngine;

public sealed class PinballRewardController
{
    private readonly BattleManager _battleManager;
    private readonly PinballBallPool _ballPool;
    private readonly PinballItemModifiers _itemModifiers;

    public PinballRewardController(
        BattleManager battleManager,
        PinballBallPool ballPool,
        PinballItemModifiers itemModifiers)
    {
        _battleManager = battleManager;
        _ballPool = ballPool;
        _itemModifiers = itemModifiers;
    }

    public PinballRewardResult ApplyBumperReward(
        Pinball ball,
        int bumperIncome,
        float comboMultiplier,
        float goldenMultiplier,
        bool grantsJackpot,
        int jackpotBaseReward,
        float jackpotIncomeMultiplier)
    {
        if (ball == null) return default;

        int safeIncome = Mathf.Max(0, bumperIncome);
        float normalReward = safeIncome *
                             Mathf.Max(0, _itemModifiers.GoldenBallReward) *
                             Mathf.Max(1f, comboMultiplier) *
                             (ball.IsGolden ? Mathf.Max(1f, goldenMultiplier) : 1f);
        int collisionReward = Mathf.Max(0, Mathf.RoundToInt(normalReward));

        int bumperReward = _itemModifiers.CalculateGoldenBumperReward(
            ball.GoldenBumperGold);
        if (bumperReward > 0)
        {
            ball.GoldenBumperGold += bumperReward;
        }

        int jackpotReward = grantsJackpot
            ? Mathf.Max(0, jackpotBaseReward) + Mathf.Max(
                0,
                Mathf.RoundToInt(safeIncome * Mathf.Max(0f, jackpotIncomeMultiplier)))
            : 0;
        int totalReward = collisionReward + bumperReward + jackpotReward;
        _battleManager.AddGold(totalReward);

        ApplySplitCapsule(ball);
        return new PinballRewardResult(totalReward, jackpotReward);
    }

    private void ApplySplitCapsule(Pinball source)
    {
        if (_itemModifiers.SplitCount <= 0 ||
            source.IsClone ||
            source.HasSplit)
        {
            return;
        }

        source.HasSplit = true;
        for (var i = 0; i < _itemModifiers.SplitCount; i++)
        {
            if (!_ballPool.TryAcquireClone(out var clone)) break;

            clone.Activate(
                source.transform.position,
                source.Velocity.normalized,
                true,
                false);
            clone.SetVelocity(
                source.Velocity *
                _itemModifiers.SplitSpeedMultiplier);
        }
    }
}

public readonly struct PinballRewardResult
{
    public int TotalReward { get; }
    public int JackpotReward { get; }

    public PinballRewardResult(int totalReward, int jackpotReward)
    {
        TotalReward = totalReward;
        JackpotReward = jackpotReward;
    }
}
